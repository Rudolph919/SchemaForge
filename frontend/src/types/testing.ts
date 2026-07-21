// Mirrors SchemaForge.Contracts/V1/Testing.

export type TestExpectationKind = 'Valid' | 'Errors'
export type TestRunStatus = 'Pending' | 'Completed'
export type ErrorSeverity = 'Error' | 'Warning'

export interface ExpectedErrorDto {
  path: string
  errorCodePattern: string
}

export interface TestExpectationDto {
  kind: TestExpectationKind
  expectedErrors: ExpectedErrorDto[] | null
}

export interface CreateTestSuiteRequest {
  name: string
  description: string | null
}

export interface CreateTestSuiteResponse {
  testSuiteId: string
}

export interface UpdateTestSuiteDetailsRequest {
  name: string
  description: string | null
}

export interface TestSuiteSummaryResponse {
  id: string
  name: string
  description: string | null
  caseCount: number
}

export interface TestCaseResponse {
  id: string
  name: string
  inputJson: unknown
  expectation: TestExpectationDto
}

export interface TestSuiteDetailResponse {
  id: string
  schemaDefinitionId: string
  name: string
  description: string | null
  cases: TestCaseResponse[]
}

export interface AddTestCaseRequest {
  name: string
  inputJson: unknown
  expectation: TestExpectationDto
}

export interface AddTestCaseResponse {
  testCaseId: string
}

export interface UpdateTestCaseRequest {
  name: string
  inputJson: unknown
  expectation: TestExpectationDto
}

export interface RunTestSuiteResponse {
  testRunId: string
}

export interface ValidationErrorResponse {
  path: string
  code: string
  message: string
  severity: ErrorSeverity
}

export interface TestCaseResultResponse {
  testCaseId: string
  testCaseName: string
  passed: boolean
  actualErrors: ValidationErrorResponse[]
}

export interface TestRunResponse {
  id: string
  testSuiteId: string
  schemaVersionId: string
  status: TestRunStatus
  executedAt: string
  results: TestCaseResultResponse[]
}
