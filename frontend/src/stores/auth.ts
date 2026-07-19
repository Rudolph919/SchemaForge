import { computed, ref } from 'vue'
import { defineStore } from 'pinia'
import { authApi } from '@/modules/auth/api/authApi'
import { tokenStorage } from '@/shared/auth/tokenStorage'
import { decodeAccessToken } from '@/shared/auth/jwt'
import type { LoginRequest, RegisterRequest } from '@/types/auth'

function claimsFromToken(token: string | null) {
  if (token === null) return { userId: null, organizationId: null, displayName: null }
  const claims = decodeAccessToken(token)
  return { userId: claims.sub, organizationId: claims.org_id, displayName: claims.name }
}

// App-wide session state (Step 7 §6) - not module-scoped, since every module needs to know
// "who's logged in and which organization are they acting as." Only the raw token is persisted
// to localStorage; userId/organizationId/displayName are always derived from its claims (not
// tracked as separate mutable fields) so there's nothing to fall out of sync on a page reload,
// where only the token string itself survives.
export const useAuthStore = defineStore('auth', () => {
  const accessToken = ref<string | null>(tokenStorage.get())

  const isAuthenticated = computed(() => accessToken.value !== null)
  const userId = computed(() => claimsFromToken(accessToken.value).userId)
  const organizationId = computed(() => claimsFromToken(accessToken.value).organizationId)
  const displayName = computed(() => claimsFromToken(accessToken.value).displayName)

  function setToken(token: string) {
    accessToken.value = token
    tokenStorage.set(token)
  }

  async function register(request: RegisterRequest) {
    // Registration doesn't itself return a token (Step 6 §2.1: register and login are separate
    // flows) - log in immediately afterward so the caller ends up with a real session either way.
    await authApi.register(request)
    await login({ email: request.email, password: request.password })
  }

  async function login(request: LoginRequest) {
    const response = await authApi.login(request)
    setToken(response.accessToken)
  }

  async function switchOrganization(organizationId: string) {
    const response = await authApi.switchOrganization({ organizationId })
    setToken(response.accessToken)
  }

  function logout() {
    accessToken.value = null
    tokenStorage.clear()
  }

  return {
    accessToken,
    userId,
    organizationId,
    displayName,
    isAuthenticated,
    register,
    login,
    switchOrganization,
    logout,
  }
})
