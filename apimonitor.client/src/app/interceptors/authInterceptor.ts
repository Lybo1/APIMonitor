import { Injectable } from '@angular/core';
import {
  HttpEvent,
  HttpInterceptor,
  HttpHandler,
  HttpRequest,
  HttpErrorResponse,
  HttpResponse
} from '@angular/common/http';
import { Observable, throwError, BehaviorSubject } from 'rxjs';
import { catchError, switchMap, filter, take } from 'rxjs/operators';
import { AuthService } from '../services/authService';

@Injectable()
  export class AuthInterceptor implements HttpInterceptor {
    private refreshingToken = false;
    private refreshTokenSubject: BehaviorSubject<boolean> = new BehaviorSubject<boolean>(false);

    constructor(private authService: AuthService,) { }

    intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
      return next.handle(req).pipe(
        catchError((error: HttpErrorResponse) => {
          if (error.status === 401 && !this.refreshingToken) {
            return this.handle401Error(req, next);
          }

          return throwError(() => error);
        })
      );
  }

  private handle401Error(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
      if (!this.refreshingToken) {
        this.refreshingToken = true;
        this.refreshTokenSubject.next(false);

        return this.authService.refreshToken().pipe(
          switchMap(() => {
            this.refreshingToken = false;
            this.refreshTokenSubject.next(true);

            return next.handle(req);
          }),
          catchError(() => {
            this.refreshingToken = false;

            return throwError(() => new Error('Session expired, please login again.'));
          })
        );
      } else {
        return this.refreshTokenSubject.pipe(
          filter((refreshed) => refreshed),
          take(1),
          switchMap(() => next.handle(req))
        );
      }
  }
}

