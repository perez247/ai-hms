import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { ApiErrorHandlerService } from './api-error-handler.service';

/**
 * Every API failure is displayed once through ApiErrorHandlerService, so components
 * never need to parse ProblemDetails themselves. The original error is rethrown so
 * local cleanup/state logic in components still runs.
 */
export const apiErrorInterceptor: HttpInterceptorFn = (request, next) => {
  const handler = inject(ApiErrorHandlerService);
  return next(request).pipe(
    catchError((error: unknown) => {
      handler.show(error);
      return throwError(() => error);
    }),
  );
};
