import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, catchError, finalize, share, switchMap, throwError } from 'rxjs';
import { TokenStorageService } from '../services/token-storage.service';
import { API_BASE_URL } from '../services/api-config';
import { HttpClient } from '@angular/common/http';
import { ApiEnvelope } from '../models/api-envelope.model';

interface RefreshResponseData {
  accessToken: string;
  refreshToken: string;
}

// Module-level so it's shared across every call to authInterceptor (one per HTTP request),
// not per-invocation — multiple requests 401ing at nearly the same moment (e.g. a page that
// fires several parallel loads on mount) must share a single in-flight /auth/refresh call.
// Without this, each 401 independently POSTs /auth/refresh; if the backend rotates/invalidates
// the refresh token on use, every refresh after the first fails and the user gets logged out
// even though the very first refresh actually succeeded.
let refreshInFlight: Observable<ApiEnvelope<RefreshResponseData>> | null = null;

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const tokenStorage = inject(TokenStorageService);
  const http = inject(HttpClient);
  const router = inject(Router);

  const token = tokenStorage.accessToken;
  const authorizedReq = token ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : req;

  return next(authorizedReq).pipe(
    catchError((error: HttpErrorResponse) => {
      const isAuthEndpoint = req.url.includes('/auth/login') || req.url.includes('/auth/refresh');
      if (error.status === 401 && !isAuthEndpoint) {
        const refreshToken = tokenStorage.refreshToken;
        if (!refreshToken) {
          tokenStorage.clear();
          router.navigate(['/login']);
          return throwError(() => error);
        }

        if (!refreshInFlight) {
          refreshInFlight = http
            .post<ApiEnvelope<RefreshResponseData>>(`${API_BASE_URL}/auth/refresh`, { refreshToken })
            .pipe(
              share(),
              finalize(() => {
                refreshInFlight = null;
              }),
            );
        }

        return refreshInFlight.pipe(
          switchMap((refreshResponse) => {
            if (refreshResponse?.success) {
              tokenStorage.saveTokens(refreshResponse.data.accessToken, refreshResponse.data.refreshToken);
              const retryReq = req.clone({ setHeaders: { Authorization: `Bearer ${refreshResponse.data.accessToken}` } });
              return next(retryReq);
            }
            tokenStorage.clear();
            router.navigate(['/login']);
            return throwError(() => error);
          }),
          catchError((refreshError) => {
            tokenStorage.clear();
            router.navigate(['/login']);
            return throwError(() => refreshError);
          }),
        );
      }
      return throwError(() => error);
    }),
  );
};
