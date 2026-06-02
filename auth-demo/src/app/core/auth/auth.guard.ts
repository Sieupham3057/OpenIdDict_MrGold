import { inject } from '@angular/core';
import {
  CanActivateFn,
  CanMatchFn,
  Router,
  ActivatedRouteSnapshot,
  RouterStateSnapshot,
} from '@angular/router';
import { AuthService } from './auth.service';

/**
 * Chặn truy cập route nếu chưa đăng nhập.
 * Lưu returnUrl để redirect về sau khi login thành công.
 */
export const authGuard: CanActivateFn = (
  _route: ActivatedRouteSnapshot,
  state: RouterStateSnapshot,
) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (auth.isAuthenticated()) return true;

  return router.createUrlTree(['/auth/login'], {
    queryParams: { returnUrl: state.url },
  });
};

/**
 * Guard kiểm tra role — dùng trong canActivate.
 */
export const roleGuard =
  (requiredRole: string): CanActivateFn =>
  () => {
    const auth = inject(AuthService);
    const router = inject(Router);

    if (auth.hasRole(requiredRole)) return true;
    return router.createUrlTree(['/forbidden']);
  };

/**
 * Guard kiểm tra role — dùng trong canMatch.
 * Chạy TRƯỚC khi lazy chunk được load, ngăn component render dù một frame.
 * Nếu user chưa đăng nhập: trả true để canActivate (authGuard) xử lý redirect về login.
 * Nếu đã đăng nhập nhưng không có role: redirect ngay về /forbidden.
 */
export const roleMatchGuard =
  (requiredRole: string): CanMatchFn =>
  () => {
    const auth = inject(AuthService);
    const router = inject(Router);

    if (!auth.isAuthenticated()) return true; // authGuard sẽ xử lý
    if (auth.hasRole(requiredRole)) return true;
    return router.createUrlTree(['/forbidden']);
  };
