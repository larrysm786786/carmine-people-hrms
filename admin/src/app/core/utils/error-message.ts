import { HttpErrorResponse } from '@angular/common/http';
import { ApiError } from '../models/api-envelope.model';

// Core services assume the backend reports failure as a 200 response with
// `{ success: false }` and throw `new Error(response.error.message)` for that case — but this
// API returns a real non-2xx status (409/404/400/...) for business failures (see
// EmployeesController.Create returning Conflict(...), for example), so Angular's HttpClient
// rejects with an HttpErrorResponse before any service ever sees `response.success`. Without
// unwrapping that here too, every such failure fell through to each caller's generic fallback
// message, hiding the backend's actual error (e.g. "Email already in use").
export function errorMessage(err: unknown): string | undefined {
  if (err instanceof HttpErrorResponse) {
    const body = err.error as ApiError | undefined;
    return body?.error?.message;
  }
  return err instanceof Error ? err.message : undefined;
}
