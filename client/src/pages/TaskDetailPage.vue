<template>
  <v-container max-width="600">
    <v-btn icon="mdi-arrow-left" variant="text" class="mb-2" @click="$router.back()" />

    <div v-if="!task" class="text-center py-8 text-medium-emphasis">
      Task not found.
    </div>

    <v-card v-else>
      <v-card-title class="d-flex align-center">
        <v-checkbox-btn
          :model-value="task.isCompleted"
          @click="tasksStore.toggleComplete(task.id)"
        />
        <span :class="{ 'text-decoration-line-through text-medium-emphasis': task.isCompleted }">
          {{ task.title }}
        </span>
      </v-card-title>

      <v-card-text>
        <div v-if="!editing">
          <p class="mb-2" v-if="task.notes">{{ task.notes }}</p>
          <p v-else class="text-medium-emphasis">No notes</p>
          <v-chip v-if="task.dueAt" size="small" prepend-icon="mdi-calendar" class="mt-2">
            Due: {{ formatDate(task.dueAt) }}
          </v-chip>
          <p class="text-caption text-medium-emphasis mt-2">
            Updated: {{ formatDateTime(task.updatedAt) }} · v{{ task.version }}
          </p>
        </div>

        <div v-else>
          <v-text-field v-model="editTitle" label="Title" class="mb-2" />
          <v-textarea v-model="editNotes" label="Notes" rows="3" class="mb-2" />
          <v-text-field v-model="editDueAt" label="Due date" type="date" />
        </div>
      </v-card-text>

      <v-card-actions>
        <v-btn
          v-if="!editing"
          color="primary"
          variant="tonal"
          prepend-icon="mdi-pencil"
          @click="startEdit"
        >
          Edit
        </v-btn>
        <template v-else>
          <v-btn @click="cancelEdit">Cancel</v-btn>
          <v-btn color="primary" @click="saveEdit">Save</v-btn>
        </template>
        <v-spacer />
        <v-btn color="error" variant="text" prepend-icon="mdi-delete" @click="confirmDelete">
          Delete
        </v-btn>
      </v-card-actions>
    </v-card>

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
import { computed, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useTasksStore } from '@/stores/tasks.store'

const props = defineProps<{ id: string }>()

const tasksStore = useTasksStore()
const router = useRouter()

const task = computed(() => tasksStore.tasks.find((t) => t.id === props.id))

const editing = ref(false)
const editTitle = ref('')
const editNotes = ref('')
const editDueAt = ref('')

function startEdit() {
  if (!task.value) return
  editTitle.value = task.value.title
  editNotes.value = task.value.notes ?? ''
  editDueAt.value = task.value.dueAt ? task.value.dueAt.split('T')[0] : ''
  editing.value = true
}

function cancelEdit() {
  editing.value = false
}

async function saveEdit() {
  if (!task.value) return
  await tasksStore.updateTask(task.value.id, {
    title: editTitle.value,
    notes: editNotes.value || null,
    dueAt: editDueAt.value || null,
  })
  editing.value = false
}

const showDelete = ref(false)

function confirmDelete() {
  showDelete.value = true
}

async function doDelete() {
  if (!task.value) return
  await tasksStore.deleteTask(task.value.id)
  router.push('/')
}

function formatDate(iso: string) {
  return new Date(iso).toLocaleDateString()
}

function formatDateTime(iso: string) {
  return new Date(iso).toLocaleString()
}
</script>
