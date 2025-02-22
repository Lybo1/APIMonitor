import { Injectable } from '@angular/core';
import { ActivatedRouteSnapshot, CanActivate, GuardResult, MaybeAsync, Router, RouterStateSnapshot } from '@angular/router';
import { Observable } from 'rxjs';
import { AuthService } from '../services/authService';
import { map, catchError } from 'rxjs/operators';

@Injectable({
  providedIn: 'root'
})

export class AuthGuard implements CanActivate {
  constructor(private authService: AuthService, private router: Router) {}

  canActivate(): Observable<boolean> {
    return this.authService.isAuthenticated().pipe(
      map((isAuthenticated) => {
        if (isAuthenticated) {
          this.router.navigate(['/login']);
          return false;
        }
        return true;
      }),
      catchError(() => {
        this.router.navigate(['/login'])
        return [false];
      })
    );
  }
}
