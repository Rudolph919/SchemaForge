import { httpClient } from '@/shared/api/httpClient'
import type {
  LoginRequest,
  LoginResponse,
  RegisterRequest,
  RegisterResponse,
  SwitchOrganizationRequest,
  SwitchOrganizationResponse,
} from '@/types/auth'

export const authApi = {
  register: (request: RegisterRequest) =>
    httpClient.post<RegisterResponse>('/api/v1/auth/register', request),

  login: (request: LoginRequest) => httpClient.post<LoginResponse>('/api/v1/auth/login', request),

  switchOrganization: (request: SwitchOrganizationRequest) =>
    httpClient.post<SwitchOrganizationResponse>('/api/v1/auth/switch-organization', request),
}
