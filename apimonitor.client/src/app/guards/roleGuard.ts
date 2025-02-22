import { Injectable } from '@angular/core';
import { ActivatedRouteSnapshot, CanActivate, GuardResult, MaybeAsync, Router, RouterStateSnapshot } from '@angular/router';
import { AuthService } from '../services/authService';
import { map } from 'rxjs/operators';
import {Observable} from 'rxjs';

@Injectable({
  providedIn: 'root'
})

export class RoleGuard implements CanActivate {
  constructor(private authService: AuthService, private router: Router) { }

  canActivate(): Observable<boolean> {
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
