import { computed, ref } from 'vue'
import { defineStore } from 'pinia'
import { authApi } from '@/modules/auth/api/authApi'
import type { LoginRequest, RegisterRequest } from '@/types/auth'

const ACCESS_TOKEN_STORAGE_KEY = 'schemaforge.accessToken'

// App-wide session state (Step 7 §6) - not module-scoped, since every module needs to know
// "who's logged in and which organization are they acting as."
export const useAuthStore = defineStore('auth', () => {
  const accessToken = ref<string | null>(localStorage.getItem(ACCESS_TOKEN_STORAGE_KEY))
  const userId = ref<string | null>(null)
  const organizationId = ref<string | null>(null)
  const displayName = ref<string | null>(null)

  const isAuthenticated = computed(() => accessToken.value !== null)

  function setSession(token: string, user: string, organization: string, name: string) {
    accessToken.value = token
    userId.value = user
    organizationId.value = organization
    displayName.value = name
    localStorage.setItem(ACCESS_TOKEN_STORAGE_KEY, token)
  }

  async function register(request: RegisterRequest) {
    // Registration doesn't itself return a token (Step 6 §2.1: register and login are separate
    // flows) - log in immediately afterward so the caller ends up with a real session either way.
    await authApi.register(request)
    await login({ email: request.email, password: request.password })
  }

  async function login(request: LoginRequest) {
    const response = await authApi.login(request)
    setSession(response.accessToken, response.userId, response.organizationId, response.displayName)
  }

  function logout() {
    accessToken.value = null
    userId.value = null
    organizationId.value = null
    displayName.value = null
    localStorage.removeItem(ACCESS_TOKEN_STORAGE_KEY)
  }

  return { accessToken, userId, organizationId, displayName, isAuthenticated, register, login, logout }
})
