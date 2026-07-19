# Development Workflow

How changes land in `main` from Phase 0 onward. The architecture design phase (`docs/architecture/01`–`10`) used direct, individually-reviewed commits to `main` — appropriate there because each commit was a single, already-approved decision with nothing left mid-flight. Code doesn't have that property, so implementation work follows a different, more conventional workflow.

## Branching

- One short-lived branch per cohesive unit of work — roughly sub-phase-sized (e.g. `phase-0/solution-scaffolding`, `phase-0/identity-vertical-slice`, `phase-1/organization-membership`), not one branch per whole roadmap phase from [10-implementation-roadmap.md](architecture/10-implementation-roadmap.md).
- Branch from `main`, open a PR once the unit of work is coherent and passes CI locally, merge, delete the branch.

## Merging

**Squash merge only.** Each PR becomes exactly one commit on `main` with a clean summary message — keeps `main`'s history linear and skimmable, and keeps in-progress/fixup commits out of the permanent record. Individual commits within a branch are still free to be as granular as useful during development; they just don't persist past the squash.

## Branch protection

GitHub's classic branch protection (required PRs, required status checks, blocked direct pushes) requires either a public repository or a paid GitHub plan — blocked on this repo's current private/free tier. The workflow above is followed **by convention, not GitHub enforcement**, for now. Revisit adding real protection if the repo ever goes public or moves to a paid plan — nothing about the workflow itself needs to change if that happens.

## CI

GitHub Actions (build + test + `ArchitectureTests` on every PR) is set up as part of Phase 0. Once it exists, every branch's changes should pass it before merging, even without a GitHub-enforced gate.
