// Mirrors SchemaForge.Contracts/V1/Audit.

export interface AuditLogEntryResponse {
  id: string
  actorUserId: string
  action: string
  entityType: string
  entityId: string
  metadataJson: string | null
  occurredAt: string
}

export interface AuditLogPageResponse {
  items: AuditLogEntryResponse[]
  totalCount: number
  page: number
  pageSize: number
}
