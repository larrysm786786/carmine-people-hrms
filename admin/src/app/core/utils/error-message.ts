import { HttpErrorResponse } from '@angular/common/http';
import { ApiError } from '../models/api-envelope.model';

// ASP.NET's [ApiController] auto-validation (FluentValidation.AspNetCore's
// AddFluentValidationAutoValidation) short-circuits the MVC pipeline itself on a failed
// request-body validator — it never reaches a controller action, so the app's own
// ExceptionHandlingMiddleware (which only catches a manually-thrown ValidationException) never
// runs either. What actually comes back is ASP.NET's stock ValidationProblemDetails shape, not
// this app's {success:false, error:{message}} envelope.
interface ValidationProblemDetails {
  title?: string;
  errors?: Record<string, string[]>;
}

// Core services assume the backend reports failure as a 200 response with
// `{ success: false }` and throw `new Error(response.error.message)` for that case — but this
// API returns a real non-2xx status (409/404/400/...) for business failures (see
// EmployeesController.Create returning Conflict(...), for example), so Angular's HttpClient
// rejects with an HttpErrorResponse before any service ever sees `response.success`. Without
// unwrapping that here too, every such failure fell through to each caller's generic fallback
// message, hiding the backend's actual error (e.g. "Email already in use").
export function errorMessage(err: unknown): string | undefined {
  if (err instanceof HttpErrorResponse) {
    const envelope = err.error as ApiError | undefined;
    if (envelope?.error?.message) return envelope.error.message;

    const problem = err.error as ValidationProblemDetails | undefined;
    if (problem?.errors) {
      const messages = Object.values(problem.errors).flat();
      if (messages.length) return messages.join(' ');
    }
    return problem?.title;
  }
  return err instanceof Error ? err.message : undefined;
}
