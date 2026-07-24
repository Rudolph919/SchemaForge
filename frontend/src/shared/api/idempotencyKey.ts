// A fresh key per logical action, reused across a manual retry of that same action (so a
// network blip followed by the user clicking the button again replays the original response
// instead of duplicating the side effect), and cleared after a confirmed success so the next,
// distinct action gets its own key.
export function useIdempotencyKey() {
  let key: string | null = null
  return {
    get: () => (key ??= crypto.randomUUID()),
    reset: () => {
      key = null
    },
  }
}

// Same idea, scoped per id - for actions triggered per-row against one of several sibling
// targets (e.g. Publish/Deprecate on a specific version), where a single shared key would
// conflate retries of unrelated targets.
export function useIdempotencyKeyMap() {
  const keys = new Map<string, string>()
  return {
    get: (id: string) => {
      let key = keys.get(id)
      if (!key) {
        key = crypto.randomUUID()
        keys.set(id, key)
      }
      return key
    },
    reset: (id: string) => {
      keys.delete(id)
    },
  }
}
