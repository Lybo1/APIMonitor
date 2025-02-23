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
   return this.authService.getUserRoles().pipe(
     map((roles) => {
       if (roles.includes('Admin') || roles.includes('User')) {
         return true;
       } else {
         this.router.navigate(['/unauthorized']);
         return false;
       }
     })
   );
  }
}
