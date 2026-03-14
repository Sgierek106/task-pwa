<template>
  <v-container max-width="800">
    <!-- Header row -->
    <v-row align="center" class="my-2">
      <v-col>
        <h1 class="text-h5">My Tasks</h1>
      </v-col>
      <v-col cols="auto" class="d-flex align-center gap-2">
        <v-chip
          :color="online ? 'success' : 'warning'"
          size="small"
          class="mr-2"
        >
          {{ online ? 'Online' : 'Offline' }}
        </v-chip>
        <v-btn
          color="primary"
          variant="tonal"
          size="small"
          :loading="syncStore.syncing"
          prepend-icon="mdi-sync"
          @click="syncStore.sync()"
        >
          Sync
        </v-btn>
        <v-btn
          color="primary"
          icon="mdi-plus"
          size="small"
          @click="showCreate = true"
        />
      </v-col>
    </v-row>

    <v-divider class="mb-4" />

    <!-- Active tasks -->
    <div v-if="tasksStore.activeTasks.length === 0 && !tasksStore.loading" class="text-center text-medium-emphasis py-8">
      <v-icon size="48" class="mb-2">mdi-check-all</v-icon>
      <p>No tasks! Add one with the + button.</p>
    </div>

    <v-list v-else lines="two">
      <v-list-item
        v-for="task in tasksStore.activeTasks"
        :key="task.id"
        :to="`/tasks/${task.id}`"
        rounded="lg"
        class="mb-1"
      >
        <template #prepend>
          <v-checkbox-btn
            :model-value="task.isCompleted"
            @click.prevent="tasksStore.toggleComplete(task.id)"
          />
        </template>

        <v-list-item-title>{{ task.title }}</v-list-item-title>
        <v-list-item-subtitle v-if="task.dueAt">
          Due: {{ formatDate(task.dueAt) }}
        </v-list-item-subtitle>

        <template #append>
          <v-btn
            icon="mdi-delete-outline"
            variant="text"
            size="small"
            color="error"
            @click.prevent="confirmDelete(task.id)"
          />
        </template>
      </v-list-item>
    </v-list>

    <!-- Completed section -->
    <v-expansion-panels v-if="tasksStore.completedTasks.length" class="mt-4">
      <v-expansion-panel title="Completed">
        <v-expansion-panel-text>
          <v-list lines="one">
            <v-list-item
              v-for="task in tasksStore.completedTasks"
              :key="task.id"
              :to="`/tasks/${task.id}`"
            >
              <template #prepend>
                <v-checkbox-btn
                  :model-value="task.isCompleted"
                  @click.prevent="tasksStore.toggleComplete(task.id)"
                />
              </template>
              <v-list-item-title class="text-decoration-line-through text-medium-emphasis">
                {{ task.title }}
              </v-list-item-title>
            </v-list-item>
          </v-list>
        </v-expansion-panel-text>
      </v-expansion-panel>
    </v-expansion-panels>

    <!-- Create task dialog -->
    <v-dialog v-model="showCreate" max-width="500">
      <v-card>
        <v-card-title>New Task</v-card-title>
        <v-card-text>
          <v-text-field
            v-model="newTitle"
            label="Title"
            autofocus
            @keyup.enter="createTask"
          />
          <v-textarea v-model="newNotes" label="Notes" rows="2" />
          <v-text-field v-model="newDueAt" label="Due date" type="date" />
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn @click="showCreate = false">Cancel</v-btn>
          <v-btn color="primary" :disabled="!newTitle.trim()" @click="createTask">Create</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <!-- Delete confirm dialog -->
    <v-dialog v-model="showDelete" max-width="400">
      <v-card>
        <v-card-title>Delete task?</v-card-title>
        <v-card-text>This cannot be undone.</v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn @click="showDelete = false">Cancel</v-btn>
          <v-btn color="error" @click="doDelete">Delete</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </v-container>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted } from 'vue'
import { useTasksStore } from '@/stores/tasks.store'
import { useSyncStore } from '@/stores/sync.store'

const tasksStore = useTasksStore()
const syncStore = useSyncStore()

const online = ref(navigator.onLine)
const onlineHandler = () => (online.value = true)
const offlineHandler = () => (online.value = false)

onMounted(() => {
  window.addEventListener('online', onlineHandler)
  window.addEventListener('offline', offlineHandler)
})

onUnmounted(() => {
  window.removeEventListener('online', onlineHandler)
  window.removeEventListener('offline', offlineHandler)
})

const showCreate = ref(false)
const newTitle = ref('')
const newNotes = ref('')
const newDueAt = ref('')

async function createTask() {
  if (!newTitle.value.trim()) return
  await tasksStore.createTask(
    newTitle.value.trim(),
    newNotes.value.trim() || undefined,
    newDueAt.value || undefined
  )
  newTitle.value = ''
  newNotes.value = ''
  newDueAt.value = ''
  showCreate.value = false
}

const showDelete = ref(false)
const deleteId = ref('')

function confirmDelete(id: string) {
  deleteId.value = id
  showDelete.value = true
}

async function doDelete() {
  await tasksStore.deleteTask(deleteId.value)
  showDelete.value = false
}

function formatDate(iso: string) {
  return new Date(iso).toLocaleDateString()
}
</script>
