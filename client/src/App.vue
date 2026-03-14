<template>
  <v-app>
    <v-app-bar color="primary" flat>
      <v-app-bar-title>
        <router-link to="/" class="text-white text-decoration-none">Task PWA</router-link>
      </v-app-bar-title>
      <template #append>
        <v-btn icon="mdi-cog" to="/settings" />
      </template>
    </v-app-bar>

    <v-main>
      <router-view />
    </v-main>

    <!-- PWA Update Snackbar -->
    <v-snackbar
      v-model="needRefresh"
      color="info"
      :timeout="-1"
      location="bottom"
    >
      A new version is available.
      <template #actions>
        <v-btn variant="text" @click="update">Refresh</v-btn>
        <v-btn variant="text" @click="needRefresh = false">Dismiss</v-btn>
      </template>
    </v-snackbar>

    <!-- Sync error snackbar -->
    <v-snackbar
      v-model="showSyncError"
      color="error"
      :timeout="5000"
      location="bottom"
    >
      {{ syncStore.lastError }}
      <template #actions>
        <v-btn variant="text" @click="showSyncError = false">Close</v-btn>
      </template>
    </v-snackbar>
  </v-app>
</template>

<script setup lang="ts">
import { ref, watch, onMounted, onUnmounted } from 'vue'
import { usePwaUpdate } from '@/pwa/registerSW'
import { useSyncStore } from '@/stores/sync.store'
import { useTasksStore } from '@/stores/tasks.store'
import { syncService } from '@/sync/syncService'

const { needRefresh, update } = usePwaUpdate()
const syncStore = useSyncStore()
const tasksStore = useTasksStore()
const showSyncError = ref(false)

watch(
  () => syncStore.lastError,
  (err) => {
    if (err) showSyncError.value = true
  }
)

let removeOnlineListener: (() => void) | null = null

onMounted(async () => {
  await syncStore.initialize()
  await tasksStore.loadTasks()

  // Auto-sync when coming online
  removeOnlineListener = syncService.setupOnlineSync((result) => {
    if (!result.success && result.error) {
      syncStore.lastError = result.error
    }
  })

  // Initial sync attempt if online
  if (navigator.onLine) {
    syncStore.sync()
  }
})

onUnmounted(() => {
  removeOnlineListener?.()
})
</script>
