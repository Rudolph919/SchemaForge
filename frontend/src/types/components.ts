// Mirrors SchemaForge.Contracts/V1/Components. Reuses SchemaNodeResponse/LocalDefinitionResponse/
// enums from types/schemas.ts directly rather than redeclaring them - the node-tree shape is
// identical between a schema version and a component version (backend Contracts does the same).
import type {
  LocalDefinitionResponse,
  SchemaLifecycleStatus,
  SchemaNodeResponse,
  VersionBumpKind,
} from '@/types/schemas'

export interface CreateComponentDefinitionRequest {
  name: string
  description: string | null
}

export interface CreateComponentDefinitionResponse {
  componentDefinitionId: string
}

export interface UpdateComponentDefinitionDetailsRequest {
  name: string
  description: string | null
}

export interface ComponentDefinitionSummaryResponse {
  id: string
  name: string
  description: string | null
}

export interface ComponentDefinitionDetailResponse {
  id: string
  organizationId: string
  name: string
  description: string | null
}

export interface CreateComponentVersionRequest {
  bumpKind: VersionBumpKind
  changeSummary: string | null
}

export interface CreateComponentVersionResponse {
  componentVersionId: string
  versionNumber: string
}

export interface ComponentVersionSummaryResponse {
  id: string
  versionNumber: string
  status: SchemaLifecycleStatus
  changeSummary: string | null
  publishedAt: string | null
}

export interface ComponentVersionDetailResponse {
  id: string
  componentDefinitionId: string
  versionNumber: string
  status: SchemaLifecycleStatus
  changeSummary: string | null
  publishedAt: string | null
  rootNode: SchemaNodeResponse
  localDefinitions: LocalDefinitionResponse[]
}
