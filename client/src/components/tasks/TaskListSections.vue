<template>
  <div
    v-if="activeTasks.length === 0 && !loading"
    class="text-center text-medium-emphasis py-8"
  >
    <v-icon size="48" class="mb-2">mdi-check-all</v-icon>
    <p>No tasks! Add one with the + button.</p>
  </div>

  <v-list v-else lines="two">
    <v-list-item
      v-for="task in activeTasks"
      :key="task.id"
      :to="`/tasks/${task.id}`"
      rounded="lg"
      class="mb-1"
    >
      <template #prepend>
        <v-checkbox-btn
          :model-value="task.isCompleted"
          @click.prevent="emit('toggle', task.id)"
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
          @click.prevent="emit('delete', task.id)"
        />
      </template>
    </v-list-item>
  </v-list>

  <v-expansion-panels v-if="completedTasks.length" class="mt-4">
    <v-expansion-panel title="Completed">
      <v-expansion-panel-text>
        <v-list lines="one">
          <v-list-item
            v-for="task in completedTasks"
            :key="task.id"
            :to="`/tasks/${task.id}`"
          >
            <template #prepend>
              <v-checkbox-btn
                :model-value="task.isCompleted"
                @click.prevent="emit('toggle', task.id)"
              />
            </template>
            <v-list-item-title
              class="text-decoration-line-through text-medium-emphasis"
            >
              {{ task.title }}
            </v-list-item-title>
          </v-list-item>
        </v-list>
      </v-expansion-panel-text>
    </v-expansion-panel>
  </v-expansion-panels>
</template>

<script setup lang="ts">
import type { Task } from "@/data/db";

defineProps<{
  activeTasks: Task[];
  completedTasks: Task[];
  loading: boolean;
}>();

const emit = defineEmits<{
  (e: "toggle", id: string): void;
  (e: "delete", id: string): void;
}>();

function formatDate(iso: string) {
  return new Date(iso).toLocaleDateString();
}
</script>
