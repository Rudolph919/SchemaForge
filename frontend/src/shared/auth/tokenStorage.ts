// Single source of truth for where the access token lives, so httpClient (which can't import
// the Pinia auth store without a circular dependency - the store itself calls into api modules
// that use httpClient) and the auth store agree on the same key and stay in sync.
const ACCESS_TOKEN_STORAGE_KEY = 'schemaforge.accessToken'

export const tokenStorage = {
  get: () => localStorage.getItem(ACCESS_TOKEN_STORAGE_KEY),
  set: (token: string) => localStorage.setItem(ACCESS_TOKEN_STORAGE_KEY, token),
  clear: () => localStorage.removeItem(ACCESS_TOKEN_STORAGE_KEY),
}
