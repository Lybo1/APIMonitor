import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import {BehaviorSubject, lastValueFrom, Observable, of} from 'rxjs';
import {catchError, map, switchMap, tap} from 'rxjs/operators';
import { Router } from '@angular/router';
import { jwtDecode } from 'jwt-decode';
import { AuthResponse, LoginRequest, RegisterRequest } from '../models/auth.model';

@Injectable({
  providedIn: 'root',
})

export class AuthService {
  private tokenKey = 'access_token';
  private apiUrl = 'https://your-api.com/api';
  private authSubject = new BehaviorSubject<boolean>(false);

  constructor(private http: HttpClient) {}

  isAuthenticated(): Observable<boolean> {
    return this.authSubject.asObservable();
  }

  login(request: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/login/login`, request, { withCredentials: true }).pipe(
      tap(() => this.authSubject.next(true))
    );
  }

  register(request: RegisterRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/register/register`, request, { withCredentials: true }).pipe(
      tap(() => this.authSubject.next(true))
    );
  }

  logout(): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/login/logout`, {}, { withCredentials: true }).pipe(
      tap(() => this.authSubject.next(false))
    );
  }

  refreshToken(): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/refresh-token/refresh`, {}, { withCredentials: true }).pipe(
      tap(() => this.authSubject.next(true))
    );
  }

  getToken(): string | null {
    return localStorage.getItem(this.tokenKey);
  }

  getUserRoles(): Observable<string[]> {
    return new Observable((observer) => {
      const token = this.getToken();

      if (!token) {
        observer.next([]);
        observer.complete();
        return;
      }

      const decodedToken: any = jwtDecode(token);
      const roles = decodedToken?.role ? (Array.isArray(decodedToken.role) ? decodedToken.role : [decodedToken.role]) : [];

      observer.next(roles);
      observer.complete();
    })
  }

  isAdmin(): Observable<boolean> {
    return this.getUserRoles().pipe(map(roles => roles.includes('Admin')));
  }

  async getCsrfToken(): Promise<string> {
    try {
      const response$ = this.http.get<{ csrfToken: string }>(`${this.apiUrl}/csrf-token`, {withCredentials: true });

      const response = await lastValueFrom(response$);

      return response?.csrfToken ?? '';
    } catch (error) {
      console.error('Failed to fetch CSRF token:', error);

      return '';
    }
  }

  async securePost<T>(endpoint: string, data: any): Promise<T> {
    const csrfToken = await this.getCsrfToken();

    if (!csrfToken) {
      throw new Error('CSRF token is missing.');
    }

   const response$ = this.http.post<T>(`${this.apiUrl}/${endpoint}`, data, {
     headers: { 'X-CSRF-Token': csrfToken },
     withCredentials: true,
   });

    return lastValueFrom(response$);
  }
}

