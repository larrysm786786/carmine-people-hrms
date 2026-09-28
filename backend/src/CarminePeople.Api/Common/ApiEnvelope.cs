using Microsoft.AspNetCore.Http;

namespace CarminePeople.Api.Common;

// Centralizes the error-response shape so every controller produces the same envelope
// (previously each controller had its own private Envelope() helper with identical shape
// but no shared implementation — this also makes it the single place to add fields like
// traceId consistently, rather than touching N call sites).
public static class ApiEnvelope
{
    public static object Error(HttpContext httpContext, string code, string message) => new
    {
        success = false,
        error = new { code, message },
        traceId = httpContext.TraceIdentifier
    };
}
