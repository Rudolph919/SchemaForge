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

async function handleResponse<TResponse>(response: Response): Promise<TResponse> {
  if (response.status === 401) {
    // The access token is missing, expired, or rejected - there's no refresh-token flow yet
    // (Step 6 §2.1 defers that), so the only correct recovery is a fresh login.
    tokenStorage.clear()
    window.location.href = '/login'
    throw new ApiError(401, { title: 'Session expired' })
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

async function request<TResponse>(path: string, method: string, body?: unknown): Promise<TResponse> {
  const response = await fetch(`${API_BASE_URL}${path}`, {
    method,
    headers: { 'Content-Type': 'application/json', ...authHeaders() },
    body: body !== undefined ? JSON.stringify(body) : undefined,
  })

  return handleResponse<TResponse>(response)
}

async function upload<TResponse>(path: string, file: File): Promise<TResponse> {
  const form = new FormData()
  form.append('file', file)

  const response = await fetch(`${API_BASE_URL}${path}`, {
    method: 'POST',
    headers: authHeaders(),
    body: form,
  })

  return handleResponse<TResponse>(response)
}

export const httpClient = {
  get: <TResponse>(path: string) => request<TResponse>(path, 'GET'),
  post: <TResponse>(path: string, body?: unknown) => request<TResponse>(path, 'POST', body),
  put: <TResponse>(path: string, body?: unknown) => request<TResponse>(path, 'PUT', body),
  delete: <TResponse>(path: string) => request<TResponse>(path, 'DELETE'),
  upload: <TResponse>(path: string, file: File) => upload<TResponse>(path, file),
}
