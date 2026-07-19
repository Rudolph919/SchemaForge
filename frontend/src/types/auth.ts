// Mirrors SchemaForge.Contracts/V1/Auth exactly. Hand-written for now; a generated TypeScript
// interface exporter (Step 6 §2.4 / Step 9 §3's export-format registry) replaces this later.

export interface RegisterRequest {
  email: string
  password: string
  displayName: string
  organizationName: string
}

export interface RegisterResponse {
  userId: string
  organizationId: string
  organizationSlug: string
}

export interface LoginRequest {
  email: string
  password: string
}

export interface LoginResponse {
  accessToken: string
  userId: string
  organizationId: string
  displayName: string
}

export interface ProblemDetails {
  title?: string
  status?: number
  detail?: string
  errorCode?: string
}
