import { defineStore } from 'pinia'
import { ref } from 'vue'
import { syncService, type SyncResult } from '@/sync/syncService'
import { metaRepo } from '@/data/metaRepo'
import { useTasksStore } from './tasks.store'

export const useSyncStore = defineStore('sync', () => {
  const syncing = ref(false)
  const lastSyncAt = ref<string | null>(null)
  const lastError = ref<string | null>(null)

  async function initialize() {
    lastSyncAt.value = await metaRepo.getLastSyncAt()
  }

  async function sync(): Promise<SyncResult> {
    syncing.value = true
    lastError.value = null
    try {
      const result = await syncService.sync()
      if (result.success) {
        lastSyncAt.value = await metaRepo.getLastSyncAt()
        // Reload tasks from IDB to pick up server changes
        const tasksStore = useTasksStore()
        await tasksStore.loadTasks()
      } else {
        lastError.value = result.error ?? 'Unknown sync error'
      }
      return result
    } finally {
      syncing.value = false
    }
  }

  return { syncing, lastSyncAt, lastError, initialize, sync }
})
