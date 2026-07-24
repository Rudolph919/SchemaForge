import type { ProblemDetails } from '@/types/api'
import { tokenStorage } from '@/shared/auth/tokenStorage'

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL as string

export class ApiError extends Error {
  status: number
  problem: ProblemDetails

  constructor(status: number, problem: ProblemDetails) {
    super(problem.detail ?? problem.title ?? 'Request failed')
    this.status = status
    this.problem = problem
  }
}

function sessionExpired(): never {
  tokenStorage.clear()
  tokenStorage.clearRefreshToken()
  window.location.href = '/login'
  throw new ApiError(401, { title: 'Session expired' })
}

// Deduped across concurrent 401s (several in-flight requests can fail at once when the access
// token expires) - only the first one actually calls /auth/refresh; the rest await its result.
// Calls the endpoint directly via fetch rather than through authApi/httpClient itself, the same
// reason tokenStorage's own comment gives for not importing the Pinia store here: this module
// can't depend on anything that depends back on it.
let refreshInFlight: Promise<boolean> | null = null

async function tryRefreshToken(): Promise<boolean> {
  const refreshToken = tokenStorage.getRefreshToken()
  if (refreshToken === null) return false

  refreshInFlight ??= (async () => {
    try {
      const response = await fetch(`${API_BASE_URL}/api/v1/auth/refresh`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ refreshToken }),
      })

      if (!response.ok) return false

      const data = (await response.json()) as { accessToken: string; refreshToken: string }
      tokenStorage.set(data.accessToken)
      tokenStorage.setRefreshToken(data.refreshToken)
      return true
    } catch {
      return false
    } finally {
      refreshInFlight = null
    }
  })()

  return refreshInFlight
}

// Wraps a request in a single silent-refresh-and-retry: a 401 triggers one attempt to trade the
// stored refresh token for a new access token, then replays the original request exactly once
// with it. If refresh fails (missing/expired/revoked refresh token), the original 401 response
// is returned as-is and the caller's existing 401 handling takes over.
async function fetchWithAuthRetry(doFetch: () => Promise<Response>): Promise<Response> {
  const response = await doFetch()
  if (response.status !== 401) return response

  const refreshed = await tryRefreshToken()
  if (!refreshed) return response

  return doFetch()
}

async function handleResponse<TResponse>(response: Response): Promise<TResponse> {
  if (response.status === 401) {
    // Silent refresh already had its one shot in fetchWithAuthRetry - a 401 reaching here means
    // there's no valid refresh token either, so a fresh login is the only recovery left.
    sessionExpired()
  }

  if (!response.ok) {
    const problem = (await response.json().catch(() => ({}))) as ProblemDetails
    throw new ApiError(response.status, problem)
  }

  if (response.status === 204) {
    return undefined as TResponse
  }

  return (await response.json()) as TResponse
}

function authHeaders(): HeadersInit {
  const token = tokenStorage.get()
  return token ? { Authorization: `Bearer ${token}` } : {}
}

async function request<TResponse>(
  path: string,
  method: string,
  body?: unknown,
  extraHeaders?: HeadersInit,
): Promise<TResponse> {
  const response = await fetchWithAuthRetry(() =>
    fetch(`${API_BASE_URL}${path}`, {
      method,
      headers: { 'Content-Type': 'application/json', ...authHeaders(), ...extraHeaders },
      body: body !== undefined ? JSON.stringify(body) : undefined,
    }),
  )

  return handleResponse<TResponse>(response)
}

// For GET endpoints backing a resource that requires If-Match on its mutating counterparts
// (Step 6 §1.5) - the caller stores the returned etag and passes it back into the next
// patch/put/delete call for that same resource.
async function requestWithETag<TResponse>(path: string): Promise<{ data: TResponse; etag: string | null }> {
  const response = await fetchWithAuthRetry(() =>
    fetch(`${API_BASE_URL}${path}`, {
      method: 'GET',
      headers: { ...authHeaders() },
    }),
  )

  const etag = response.headers.get('ETag')
  const data = await handleResponse<TResponse>(response)
  return { data, etag }
}

// For endpoints returning raw generated text (export/documentation formats) rather than JSON -
// json-schema/openapi ARE valid JSON on the wire, but typescript/csharp/markdown/html aren't, so
// this always reads as plain text and lets the caller parse further if it wants to.
async function requestText(path: string): Promise<string> {
  const response = await fetchWithAuthRetry(() =>
    fetch(`${API_BASE_URL}${path}`, {
      method: 'GET',
      headers: { ...authHeaders() },
    }),
  )

  if (response.status === 401) {
    sessionExpired()
  }

  if (!response.ok) {
    const problem = (await response.json().catch(() => ({}))) as ProblemDetails
    throw new ApiError(response.status, problem)
  }

  return response.text()
}

async function upload<TResponse>(path: string, file: File, idempotencyKey?: string): Promise<TResponse> {
  const form = new FormData()
  form.append('file', file)

  const response = await fetchWithAuthRetry(() =>
    fetch(`${API_BASE_URL}${path}`, {
      method: 'POST',
      headers: { ...authHeaders(), ...idempotencyKeyHeader(idempotencyKey) },
      body: form,
    }),
  )

  return handleResponse<TResponse>(response)
}

function ifMatchHeader(ifMatch?: string): HeadersInit | undefined {
  return ifMatch !== undefined ? { 'If-Match': ifMatch } : undefined
}

// Only meaningful on the POST endpoints the backend has marked [Idempotent] (Step 6 §1.6) - a
// client retry carrying the same key replays the original response instead of re-executing the
// side effect. The caller is responsible for reusing the same key across a manual retry of the
// same logical action and getting a fresh one for the next, distinct action (see
// shared/api/idempotencyKey.ts).
function idempotencyKeyHeader(idempotencyKey?: string): HeadersInit | undefined {
  return idempotencyKey !== undefined ? { 'Idempotency-Key': idempotencyKey } : undefined
}

export const httpClient = {
  get: <TResponse>(path: string) => request<TResponse>(path, 'GET'),
  getWithETag: <TResponse>(path: string) => requestWithETag<TResponse>(path),
  post: <TResponse>(path: string, body?: unknown, idempotencyKey?: string) =>
    request<TResponse>(path, 'POST', body, idempotencyKeyHeader(idempotencyKey)),
  put: <TResponse>(path: string, body?: unknown, ifMatch?: string) =>
    request<TResponse>(path, 'PUT', body, ifMatchHeader(ifMatch)),
  patch: <TResponse>(path: string, body?: unknown, ifMatch?: string) =>
    request<TResponse>(path, 'PATCH', body, ifMatchHeader(ifMatch)),
  delete: <TResponse>(path: string, ifMatch?: string) =>
    request<TResponse>(path, 'DELETE', undefined, ifMatchHeader(ifMatch)),
  upload: <TResponse>(path: string, file: File, idempotencyKey?: string) => upload<TResponse>(path, file, idempotencyKey),
  getText: (path: string) => requestText(path),
}
