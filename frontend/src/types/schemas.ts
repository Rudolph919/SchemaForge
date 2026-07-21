// Mirrors SchemaForge.Contracts/V1/Schemas.

export type SchemaLifecycleStatus = 'Draft' | 'Published' | 'Deprecated'
export type VersionBumpKind = 'Major' | 'Minor' | 'Patch'
export type NodeKind = 'Object' | 'Array' | 'String' | 'Number' | 'Integer' | 'Boolean' | 'Null'
export type CompositionKind = 'OneOf' | 'AnyOf' | 'AllOf' | 'Not'
export type NodeAttachmentKind =
  | 'ObjectProperty'
  | 'ArrayPrefixItem'
  | 'ArrayItems'
  | 'CompositionBranch'
  | 'ConditionalIf'
  | 'ConditionalThen'
  | 'ConditionalElse'
export type SchemaFormat =
  | 'Date'
  | 'DateTime'
  | 'Time'
  | 'Email'
  | 'Hostname'
  | 'Ipv4'
  | 'Ipv6'
  | 'Uri'
  | 'UriReference'
  | 'Uuid'
  | 'Custom'
export type VersionConstraintKind = 'ExactVersion' | 'MinimumVersion' | 'Latest'

export interface CreateSchemaDefinitionRequest {
  name: string
  description: string | null
}

export interface CreateSchemaDefinitionResponse {
  schemaDefinitionId: string
}

export interface UpdateSchemaDefinitionDetailsRequest {
  name: string
  description: string | null
  tags: string[]
}

export interface SchemaDefinitionSummaryResponse {
  id: string
  name: string
  description: string | null
  tags: string[]
}

export interface SchemaDefinitionDetailResponse {
  id: string
  projectId: string
  name: string
  description: string | null
  tags: string[]
}

export interface CreateSchemaVersionRequest {
  bumpKind: VersionBumpKind
  changeSummary: string | null
}

export interface CreateSchemaVersionResponse {
  schemaVersionId: string
  versionNumber: string
}

export interface SchemaVersionSummaryResponse {
  id: string
  versionNumber: string
  status: SchemaLifecycleStatus
  changeSummary: string | null
  publishedAt: string | null
}

export interface ObjectConstraintsDto {
  minProperties: number | null
  maxProperties: number | null
  additionalPropertiesAllowed: boolean
}

export interface ArrayConstraintsDto {
  minItems: number | null
  maxItems: number | null
  uniqueItems: boolean
}

export interface StringConstraintsDto {
  minLength: number | null
  maxLength: number | null
  pattern: string | null
  format: SchemaFormat | null
  customFormatValue: string | null
}

export interface NumericConstraintsDto {
  minimum: number | null
  maximum: number | null
  exclusiveMinimum: boolean
  exclusiveMaximum: boolean
  multipleOf: number | null
}

export interface VersionConstraintDto {
  kind: VersionConstraintKind
  version: string | null
}

export interface ComponentReferenceDto {
  componentVersionId: string
  constraint: VersionConstraintDto
}

// JSON literal fields (Examples/DefaultValue/AllowedValues/ConstValue) are unknown JSON values on
// the wire, matching the backend's use of System.Text.Json.JsonElement directly (Step 6 §2.4).
export interface SchemaNodeResponse {
  id: string
  propertyName: string | null
  order: number
  kind: NodeKind | null
  description: string | null
  notes: string | null
  isNullable: boolean
  isRequiredByParent: boolean
  examples: unknown[]
  defaultValue: unknown
  allowedValues: unknown[] | null
  constValue: unknown
  objectConstraints: ObjectConstraintsDto | null
  arrayConstraints: ArrayConstraintsDto | null
  stringConstraints: StringConstraintsDto | null
  numericConstraints: NumericConstraintsDto | null
  properties: SchemaNodeResponse[]
  prefixItems: SchemaNodeResponse[]
  itemsNode: SchemaNodeResponse | null
  dependentRequired: Record<string, string[]> | null
  composition: CompositionKind | null
  compositionBranches: SchemaNodeResponse[]
  ifNode: SchemaNodeResponse | null
  thenNode: SchemaNodeResponse | null
  elseNode: SchemaNodeResponse | null
  componentReference: ComponentReferenceDto | null
  localDefinitionRef: string | null
}

export interface LocalDefinitionResponse {
  id: string
  name: string
  rootNode: SchemaNodeResponse
}

export interface SchemaVersionDetailResponse {
  id: string
  schemaDefinitionId: string
  versionNumber: string
  status: SchemaLifecycleStatus
  changeSummary: string | null
  publishedAt: string | null
  rootNode: SchemaNodeResponse
  localDefinitions: LocalDefinitionResponse[]
}

export interface AddSchemaNodeRequest {
  parentNodeId: string
  attachmentKind: NodeAttachmentKind
  propertyName: string | null
  kind: NodeKind | null
}

export interface AddSchemaNodeResponse {
  nodeId: string
}

export interface UpdateSchemaNodeRequest {
  kind: NodeKind | null
  description: string | null
  notes: string | null
  isNullable: boolean
  isRequiredByParent: boolean
  examples: unknown[]
  defaultValue: unknown
  allowedValues: unknown[] | null
  constValue: unknown
  objectConstraints: ObjectConstraintsDto | null
  arrayConstraints: ArrayConstraintsDto | null
  stringConstraints: StringConstraintsDto | null
  numericConstraints: NumericConstraintsDto | null
  dependentRequired: Record<string, string[]> | null
  composition: CompositionKind | null
  componentReference: ComponentReferenceDto | null
  localDefinitionRef: string | null
}

export interface MoveSchemaNodeRequest {
  newOrder: number
}

export interface SchemaDiffChangeResponse {
  path: string
  changes: string[]
}

export interface SchemaDiffResponse {
  addedPaths: string[]
  removedPaths: string[]
  changedPaths: SchemaDiffChangeResponse[]
}

// Step 9 §2: a suggestion is never persisted server-side, so the client holds the full tree
// in memory and resends it verbatim to /versions/from-suggestion alongside accepted node ids.
export interface SuggestedNodeResponse {
  id: string
  propertyName: string | null
  kind: NodeKind
  description: string | null
  confidence: number
  children: SuggestedNodeResponse[]
}

export interface SchemaSuggestionResponse {
  providerName: string
  overallConfidence: number | null
  nodes: SuggestedNodeResponse[]
}

export interface CreateDraftFromSuggestionRequest {
  suggestion: SchemaSuggestionResponse
  acceptedNodeIds: string[]
  bumpKind: VersionBumpKind
  changeSummary: string | null
}

export interface CreateDraftFromSuggestionResponse {
  schemaVersionId: string
  versionNumber: string
  acceptedCount: number
}
