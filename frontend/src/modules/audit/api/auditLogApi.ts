import { httpClient } from '@/shared/api/httpClient'
import type { AuditLogPageResponse } from '@/types/audit'

export interface AuditLogFilters {
  entityType?: string
  entityId?: string
  actorUserId?: string
  occurredFrom?: string
  occurredTo?: string
  page: number
  pageSize: number
}

export const auditLogApi = {
  list: (filters: AuditLogFilters) => {
    const params = new URLSearchParams()
    if (filters.entityType) params.set('entityType', filters.entityType)
    if (filters.entityId) params.set('entityId', filters.entityId)
    if (filters.actorUserId) params.set('actorUserId', filters.actorUserId)
    if (filters.occurredFrom) params.set('occurredFrom', filters.occurredFrom)
    if (filters.occurredTo) params.set('occurredTo', filters.occurredTo)
    params.set('page', String(filters.page))
    params.set('pageSize', String(filters.pageSize))

    return httpClient.get<AuditLogPageResponse>(`/api/v1/audit-log?${params.toString()}`)
  },
}
