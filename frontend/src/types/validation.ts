// Mirrors SchemaForge.Contracts/V1/Validation.

export type ValidationOutcome = 'Valid' | 'Invalid'
export type ErrorSeverity = 'Error' | 'Warning'

export interface ValidationErrorResponse {
  path: string
  code: string
  message: string
  severity: ErrorSeverity
}

export interface ValidateJsonPayloadResponse {
  validationRunId: string
  outcome: ValidationOutcome
  errors: ValidationErrorResponse[]
}

export interface ValidationRunSummaryResponse {
  id: string
  outcome: ValidationOutcome
  errors: ValidationErrorResponse[]
  executedAt: string
  executedByUserId: string
}
