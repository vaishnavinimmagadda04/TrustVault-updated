import { HttpInterceptorFn } from '@angular/common/http';

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const stored = sessionStorage.getItem('trustvault.session');
  if (!stored) return next(request);
  try {
    const token = (JSON.parse(stored) as { token?: string }).token;
    return token ? next(request.clone({ setHeaders: { Authorization: `Bearer ${token}` } })) : next(request);
  } catch {
    sessionStorage.removeItem('trustvault.session');
    return next(request);
  }
};
