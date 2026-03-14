<template>
  <v-container max-width="600">
    <h1 class="text-h5 mb-4">Settings</h1>

    <!-- User Key section -->
    <v-card class="mb-4">
      <v-card-title>User Key</v-card-title>
      <v-card-text>
        <p class="text-body-2 mb-3 text-medium-emphasis">
          Your user key partitions your tasks on the server. Use the same key on all devices to
          enable multi-device sync.
        </p>

        <v-text-field
          :model-value="userKey ?? '(not set)'"
          label="Current User Key"
          readonly
          variant="outlined"
          append-inner-icon="mdi-content-copy"
          @click:append-inner="copyUserKey"
        />

        <v-text-field
          v-model="newUserKey"
          label="Paste a User Key to use"
          variant="outlined"
          hint="Paste a key from another device to sync across devices"
          persistent-hint
          class="mt-2"
        />
      </v-card-text>
      <v-card-actions>
        <v-btn color="primary" variant="tonal" @click="generateNewKey">
          Generate New Key
        </v-btn>
        <v-btn
          color="success"
          :disabled="!isValidUUID(newUserKey)"
          @click="applyPastedKey"
        >
          Apply Pasted Key
        </v-btn>
      </v-card-actions>
    </v-card>

    <!-- Client ID section -->
    <v-card class="mb-4">
      <v-card-title>Device Info</v-card-title>
      <v-card-text>
        <v-text-field
          :model-value="clientId ?? 'Loading...'"
          label="Client ID (this device)"
          readonly
          variant="outlined"
          append-inner-icon="mdi-content-copy"
          @click:append-inner="copyClientId"
        />
      </v-card-text>
    </v-card>

    <!-- Sync info -->
    <v-card class="mb-4">
      <v-card-title>Sync Status</v-card-title>
      <v-card-text>
        <p class="text-body-2">
          Last synced:
          <strong>{{ lastSyncAt ? new Date(lastSyncAt).toLocaleString() : 'Never' }}</strong>
        </p>
        <p class="text-body-2 mt-1">
          Pending ops in outbox: <strong>{{ pendingCount }}</strong>
        </p>
      </v-card-text>
      <v-card-actions>
        <v-btn
          color="primary"
          prepend-icon="mdi-sync"
          :loading="syncStore.syncing"
          @click="syncNow"
        >
          Sync Now
        </v-btn>
      </v-card-actions>
    </v-card>

    <!-- Snackbar feedback -->
    <v-snackbar v-model="snackbar" :color="snackbarColor" :timeout="3000">
      {{ snackbarText }}
    </v-snackbar>
  </v-container>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { metaRepo } from '@/data/metaRepo'
import { outboxRepo } from '@/data/outboxRepo'
import { useSyncStore } from '@/stores/sync.store'

const syncStore = useSyncStore()

const userKey = ref<string | null>(null)
const clientId = ref<string | null>(null)
const lastSyncAt = ref<string | null>(null)
const pendingCount = ref(0)
const newUserKey = ref('')

const snackbar = ref(false)
const snackbarText = ref('')
const snackbarColor = ref('success')

function notify(text: string, color = 'success') {
  snackbarText.value = text
  snackbarColor.value = color
  snackbar.value = true
}

onMounted(async () => {
  userKey.value = await metaRepo.getUserKey()
  clientId.value = await metaRepo.getClientId()
  lastSyncAt.value = await metaRepo.getLastSyncAt()
  const ops = await outboxRepo.getPending()
  pendingCount.value = ops.length
})

async function generateNewKey() {
  const key = crypto.randomUUID()
  await metaRepo.setUserKey(key)
  userKey.value = key
  notify('New user key generated')
}

async function applyPastedKey() {
  if (!isValidUUID(newUserKey.value)) return
  await metaRepo.setUserKey(newUserKey.value)
  userKey.value = newUserKey.value
  newUserKey.value = ''
  notify('User key updated')
}

async function copyUserKey() {
  if (!userKey.value) return
  await navigator.clipboard.writeText(userKey.value)
  notify('User key copied to clipboard')
}

async function copyClientId() {
  if (!clientId.value) return
  await navigator.clipboard.writeText(clientId.value)
  notify('Client ID copied to clipboard')
}

async function syncNow() {
  const result = await syncStore.sync()
  if (result.success) {
    lastSyncAt.value = await metaRepo.getLastSyncAt()
    const ops = await outboxRepo.getPending()
    pendingCount.value = ops.length
    notify(`Sync complete. Applied: ${result.appliedCount}, Changed: ${result.changedCount}`)
  } else {
    notify(result.error ?? 'Sync failed', 'error')
  }
}

function isValidUUID(value: string): boolean {
  return /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(value)
}
</script>
