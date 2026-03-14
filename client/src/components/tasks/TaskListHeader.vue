<template>
  <v-row align="center" class="my-2">
    <v-col>
      <h1 class="text-h5">My Tasks</h1>
    </v-col>
    <v-col cols="auto" class="d-flex align-center gap-2">
      <v-chip :color="online ? 'success' : 'warning'" size="small" class="mr-2">
        {{ online ? "Online" : "Offline" }}
      </v-chip>
      <v-btn
        color="primary"
        variant="tonal"
        size="small"
        :loading="syncing"
        prepend-icon="mdi-sync"
        @click="emit('sync')"
      >
        Sync
      </v-btn>
      <v-btn
        color="primary"
        icon="mdi-plus"
        size="small"
        @click="emit('add')"
      />
    </v-col>
  </v-row>
</template>

<script setup lang="ts">
defineProps<{
  online: boolean;
  syncing: boolean;
}>();

const emit = defineEmits<{
  (e: "sync"): void;
  (e: "add"): void;
}>();
</script>
