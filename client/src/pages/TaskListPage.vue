<template>
  <v-container max-width="800">
    <TaskListHeader
      :online="online"
      :syncing="syncStore.syncing"
      @sync="syncStore.sync()"
      @add="showCreate = true"
    />

    <v-divider class="mb-4" />

    <TaskListSections
      :active-tasks="tasksStore.activeTasks"
      :completed-tasks="tasksStore.completedTasks"
      :loading="tasksStore.loading"
      @toggle="tasksStore.toggleComplete"
      @delete="confirmDelete"
    />

    <TaskCreateDialog v-model="showCreate" @create="createTask" />

    <ConfirmDeleteDialog v-model="showDelete" @confirm="doDelete" />
  </v-container>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted } from "vue";
import { useTasksStore } from "@/stores/tasks.store";
import { useSyncStore } from "@/stores/sync.store";
import TaskListHeader from "@/components/tasks/TaskListHeader.vue";
import TaskListSections from "@/components/tasks/TaskListSections.vue";
import TaskCreateDialog from "@/components/tasks/TaskCreateDialog.vue";
import ConfirmDeleteDialog from "@/components/tasks/ConfirmDeleteDialog.vue";

const tasksStore = useTasksStore();
const syncStore = useSyncStore();

const online = ref(navigator.onLine);
const onlineHandler = () => (online.value = true);
const offlineHandler = () => (online.value = false);

onMounted(() => {
  window.addEventListener("online", onlineHandler);
  window.addEventListener("offline", offlineHandler);
});

onUnmounted(() => {
  window.removeEventListener("online", onlineHandler);
  window.removeEventListener("offline", offlineHandler);
});

const showCreate = ref(false);

async function createTask(payload: {
  title: string;
  notes?: string;
  dueAt?: string;
}) {
  await tasksStore.createTask(payload.title, payload.notes, payload.dueAt);
}

const showDelete = ref(false);
const deleteId = ref("");

function confirmDelete(id: string) {
  deleteId.value = id;
  showDelete.value = true;
}

async function doDelete() {
  await tasksStore.deleteTask(deleteId.value);
  showDelete.value = false;
}
</script>
