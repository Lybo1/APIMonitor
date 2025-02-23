import { Injectable } from '@angular/core';
import { CanActivate, CanActivateChild, Router } from '@angular/router';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { AuthService } from '../services/authService';

@Injectable({
  providedIn: 'root'
})

export class AdminGuard implements CanActivate, CanActivateChild {
  constructor(private authService: AuthService, private router: Router) {}

  canActivate(): Observable<boolean> {
    return this.checkAdminAccess();
  }

  canActivateChild(): Observable<boolean> {
    return this.checkAdminAccess();
  }

  private checkAdminAccess(): Observable<boolean> {
    return this.authService.getUserRoles().pipe(
      map((roles) => {
        if (roles.includes('Admin')) {
          return true;
        } else {
          this.router.navigate(['/unauthorized']);
          return false;
        }
      })
    );
  }
}
