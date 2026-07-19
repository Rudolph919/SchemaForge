# SchemaForge Frontend

Vue 3 + TypeScript + Vite + Pinia + Tailwind CSS.

## Development

```bash
npm install
npm run dev
```

Requires the backend API running and reachable at `VITE_API_BASE_URL` (see `.env.development` —
defaults to `http://localhost:5001`, matching `dotnet run`'s default HTTP profile from
`src/SchemaForge.Api/Properties/launchSettings.json`). Start it separately:

```bash
dotnet run --project ../src/SchemaForge.Api
```

## Structure

Mirrors the backend's module boundaries where practical (Step 7 §6 of the architecture docs):

- `src/modules/<name>/` — feature modules (views, module-local API calls)
- `src/shared/` — cross-module UI primitives and the base HTTP client
- `src/stores/` — app-wide Pinia stores (session/auth, not module-scoped)
- `src/router/` — route definitions and auth guards
- `src/types/` — TypeScript interfaces mirroring `SchemaForge.Contracts`

## Build

```bash
npm run build
```

Type-checks with `vue-tsc` and bundles with Vite.
