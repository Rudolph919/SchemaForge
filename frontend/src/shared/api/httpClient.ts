import type { ProblemDetails } from '@/types/auth'

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

async function request<TResponse>(
  path: string,
  method: string,
  body?: unknown,
): Promise<TResponse> {
  const response = await fetch(`${API_BASE_URL}${path}`, {
    method,
    headers: { 'Content-Type': 'application/json' },
    body: body !== undefined ? JSON.stringify(body) : undefined,
  })

  if (!response.ok) {
    const problem = (await response.json().catch(() => ({}))) as ProblemDetails
    throw new ApiError(response.status, problem)
  }

  return (await response.json()) as TResponse
}

export const httpClient = {
  post: <TResponse>(path: string, body: unknown) => request<TResponse>(path, 'POST', body),
}
