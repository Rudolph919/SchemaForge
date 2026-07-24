// Single source of truth for where the access/refresh tokens live, so httpClient (which can't
// import the Pinia auth store without a circular dependency - the store itself calls into api
// modules that use httpClient) and the auth store agree on the same keys and stay in sync.
const ACCESS_TOKEN_STORAGE_KEY = 'schemaforge.accessToken'
const REFRESH_TOKEN_STORAGE_KEY = 'schemaforge.refreshToken'

export const tokenStorage = {
  get: () => localStorage.getItem(ACCESS_TOKEN_STORAGE_KEY),
  set: (token: string) => localStorage.setItem(ACCESS_TOKEN_STORAGE_KEY, token),
  clear: () => localStorage.removeItem(ACCESS_TOKEN_STORAGE_KEY),
  getRefreshToken: () => localStorage.getItem(REFRESH_TOKEN_STORAGE_KEY),
  setRefreshToken: (token: string) => localStorage.setItem(REFRESH_TOKEN_STORAGE_KEY, token),
  clearRefreshToken: () => localStorage.removeItem(REFRESH_TOKEN_STORAGE_KEY),
}
