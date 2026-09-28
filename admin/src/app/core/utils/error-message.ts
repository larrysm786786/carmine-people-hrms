// Every core service throws `new Error(response.error.message)` on a failed API call
// (see e.g. organization.service.ts), so `err instanceof Error` covers the real shape at
// every catch site in this app — this just gives that extraction a single, correctly-typed
// place instead of each component declaring `catch (err: any)` and reading `err.message`.
export function errorMessage(err: unknown): string | undefined {
  return err instanceof Error ? err.message : undefined;
}
