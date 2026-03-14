import { outboxRepo } from '@/data/outboxRepo'
import { tasksRepo } from '@/data/tasksRepo'
import type { Task } from '@/data/db'
import { metaRepo } from '@/data/metaRepo'

const API_BASE = import.meta.env.VITE_API_BASE ?? 'http://localhost:5000'

export interface SyncResult {
  success: boolean
  appliedCount: number
  changedCount: number
  error?: string
}

let syncInProgress = false

export const syncService = {
  async sync(): Promise<SyncResult> {
    if (syncInProgress) {
      return { success: false, appliedCount: 0, changedCount: 0, error: 'Sync already in progress' }
    }
    syncInProgress = true
    try {
      const userKey = await metaRepo.getUserKey()
      if (!userKey) {
        return { success: false, appliedCount: 0, changedCount: 0, error: 'No user key configured. Set one in Settings.' }
      }

      const clientId = await metaRepo.getClientId()
      const lastSyncAt = await metaRepo.getLastSyncAt()
      const pendingOps = await outboxRepo.getPending()

      const body = {
        clientId,
        lastSyncAt: lastSyncAt ?? null,
        operations: pendingOps.map((op) => ({
          opId: op.opId,
          type: op.type,
          entityId: op.entityId,
          createdAt: op.createdAt,
          task: op.task
            ? {
                id: op.task.id,
                title: op.task.title,
                notes: op.task.notes,
                isCompleted: op.task.isCompleted,
                dueAt: op.task.dueAt,
                updatedAt: op.task.updatedAt,
                deletedAt: op.task.deletedAt,
                baseVersion: null,
              }
            : null,
        })),
      }

      const response = await fetch(`${API_BASE}/api/sync`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'X-User-Key': userKey,
        },
        body: JSON.stringify(body),
      })

      if (!response.ok) {
        const text = await response.text()
        return { success: false, appliedCount: 0, changedCount: 0, error: `Server error ${response.status}: ${text}` }
      }

      const result = await response.json() as {
        serverTime: string
        appliedOpIds: string[]
        rejected: Array<{ opId: string; reason: string }>
        changedTasks: Array<Task & { version: number }>
      }

      // Remove applied ops from outbox
      if (result.appliedOpIds?.length) {
        await outboxRepo.remove(result.appliedOpIds)
      }

      // Apply changed tasks to local IDB
      if (result.changedTasks?.length) {
        await tasksRepo.upsertMany(
          result.changedTasks.map((t) => ({
            id: t.id,
            title: t.title,
            notes: t.notes,
            isCompleted: t.isCompleted,
            dueAt: t.dueAt,
            updatedAt: t.updatedAt,
            deletedAt: t.deletedAt,
            version: t.version,
          }))
        )
      }

      // Update watermark to server time
      await metaRepo.setLastSyncAt(result.serverTime)

      return {
        success: true,
        appliedCount: result.appliedOpIds?.length ?? 0,
        changedCount: result.changedTasks?.length ?? 0,
      }
    } catch (err) {
      const message = err instanceof Error ? err.message : String(err)
      return { success: false, appliedCount: 0, changedCount: 0, error: message }
    } finally {
      syncInProgress = false
    }
  },

  setupOnlineSync(onSync: (result: SyncResult) => void): () => void {
    const handler = async () => {
      if (navigator.onLine) {
        const result = await syncService.sync()
        onSync(result)
      }
    }
    window.addEventListener('online', handler)
    return () => window.removeEventListener('online', handler)
  },
}
