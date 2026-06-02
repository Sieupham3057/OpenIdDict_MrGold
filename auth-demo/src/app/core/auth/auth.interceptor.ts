import {
  HttpInterceptorFn,
  HttpRequest,
  HttpHandlerFn,
  HttpErrorResponse,
} from '@angular/common/http';
import { inject } from '@angular/core';
import { throwError } from 'rxjs';
import { catchError, switchMap } from 'rxjs/operators';
import { TokenService } from './token.service';
import { AuthService } from './auth.service';
import { Router } from '@angular/router';

/**
 * HTTP Interceptor — tự động đính kèm Bearer token vào mọi request.
 *
 * Khi nhận 401:
 * 1. Thử refresh token (dùng HttpOnly cookie, Angular không cần biết giá trị)
 * 2. Nếu refresh thành công → retry request gốc với access token mới
 * 3. Nếu refresh thất bại → redirect về login
 *
 * Tại sao an toàn:
 * - Access token lấy từ memory, không phải localStorage
 * - Refresh token nằm trong HttpOnly cookie, interceptor không biết giá trị
 */
export const authInterceptor: HttpInterceptorFn = (
  req: HttpRequest<unknown>,
  next: HttpHandlerFn,
) => {
  const tokenService = inject(TokenService);
  const authService = inject(AuthService);
  const router = inject(Router);

  const token = tokenService.getAccessToken();
  const authReq = attachToken(req, token);

  return next(authReq).pipe(
    catchError(error => {
      if (!(error instanceof HttpErrorResponse)) return throwError(() => error);

      if (error.status === 401) {
        // Không retry nếu đang gọi chính token endpoint (tránh vòng lặp vô hạn)
        if (req.url.includes('/connect/token')) {
          router.navigate(['/auth/login']);
          return throwError(() => error);
        }

        // Thử refresh → retry request gốc
        return authService.refreshAccessToken().pipe(
          switchMap(res => next(attachToken(req, res.access_token))),
          catchError(refreshErr => {
            router.navigate(['/auth/login']);
            return throwError(() => refreshErr);
          }),
        );
      }

      if (error.status === 403) {
        router.navigate(['/forbidden']);
      }

      return throwError(() => error);
    }),
  );
};

function attachToken(req: HttpRequest<unknown>, token: string | null): HttpRequest<unknown> {
  if (!token) return req;
  return req.clone({ setHeaders: { Authorization: `Bearer ${token}` } });
}
