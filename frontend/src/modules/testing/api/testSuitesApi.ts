import { httpClient } from '@/shared/api/httpClient'
import type {
  AddTestCaseRequest,
  AddTestCaseResponse,
  CreateTestSuiteRequest,
  CreateTestSuiteResponse,
  RunTestSuiteResponse,
  TestRunResponse,
  TestSuiteDetailResponse,
  TestSuiteSummaryResponse,
  UpdateTestCaseRequest,
  UpdateTestSuiteDetailsRequest,
} from '@/types/testing'

export const testSuitesApi = {
  listSuites: (schemaDefinitionId: string) =>
    httpClient.get<TestSuiteSummaryResponse[]>(`/api/v1/schemas/${schemaDefinitionId}/test-suites`),

  createSuite: (schemaDefinitionId: string, request: CreateTestSuiteRequest) =>
    httpClient.post<CreateTestSuiteResponse>(`/api/v1/schemas/${schemaDefinitionId}/test-suites`, request),

  getSuite: (testSuiteId: string) => httpClient.get<TestSuiteDetailResponse>(`/api/v1/test-suites/${testSuiteId}`),

  updateSuiteDetails: (testSuiteId: string, request: UpdateTestSuiteDetailsRequest) =>
    httpClient.patch<void>(`/api/v1/test-suites/${testSuiteId}`, request),

  addCase: (testSuiteId: string, request: AddTestCaseRequest) =>
    httpClient.post<AddTestCaseResponse>(`/api/v1/test-suites/${testSuiteId}/cases`, request),

  updateCase: (testSuiteId: string, caseId: string, request: UpdateTestCaseRequest) =>
    httpClient.patch<void>(`/api/v1/test-suites/${testSuiteId}/cases/${caseId}`, request),

  removeCase: (testSuiteId: string, caseId: string) =>
    httpClient.delete<void>(`/api/v1/test-suites/${testSuiteId}/cases/${caseId}`),

  // Backend returns 202 Accepted - handled identically to any other JSON response by httpClient
  // (it only special-cases 204/401), so no separate method is needed here.
  run: (testSuiteId: string, targetVersionId: string) =>
    httpClient.post<RunTestSuiteResponse>(`/api/v1/test-suites/${testSuiteId}/run?targetVersionId=${targetVersionId}`),

  getRun: (testRunId: string) => httpClient.get<TestRunResponse>(`/api/v1/test-runs/${testRunId}`),
}
