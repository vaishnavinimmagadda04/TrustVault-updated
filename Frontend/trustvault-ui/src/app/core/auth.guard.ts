import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

export const authGuard: CanActivateFn = () => {
  const router = inject(Router);
  return inject(AuthService).session ? true : router.createUrlTree(['/login']);
};

export const uploadGuard: CanActivateFn = () => {
  const router = inject(Router);
  const session = inject(AuthService).session;
  return session && session.role !== 'Viewer' ? true : router.createUrlTree(['/']);
};
