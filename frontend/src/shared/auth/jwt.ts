export interface AccessTokenClaims {
  sub: string
  org_id: string
  name: string
}

// Decoding only, never verification - the token was already validated server-side when issued
// and is re-validated by the Api on every request; this just reads the claims back out so the
// frontend doesn't need a second source of truth (a separate userId/orgId/displayName response
// field) that could drift from what's actually in the token, especially across a page reload
// where only the raw token string survives in localStorage.
export function decodeAccessToken(token: string): AccessTokenClaims {
  const payload = token.split('.')[1]
  const base64 = payload.replace(/-/g, '+').replace(/_/g, '/')
  const json = decodeURIComponent(
    atob(base64)
      .split('')
      .map((c) => '%' + c.charCodeAt(0).toString(16).padStart(2, '0'))
      .join(''),
  )
  return JSON.parse(json) as AccessTokenClaims
}
