<template>
  <v-dialog v-model="open" max-width="500">
    <v-card>
      <v-card-title>New Task</v-card-title>
      <v-card-text>
        <v-text-field
          v-model="title"
          label="Title"
          autofocus
          @keyup.enter="submit"
        />
        <v-textarea v-model="notes" label="Notes" rows="2" />
        <v-text-field v-model="dueAt" label="Due date" type="date" />
      </v-card-text>
      <v-card-actions>
        <v-spacer />
        <v-btn @click="open = false">Cancel</v-btn>
        <v-btn color="primary" :disabled="!title.trim()" @click="submit"
          >Create</v-btn
        >
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>

<script setup lang="ts">
import { computed, ref, watch } from "vue";

const props = defineProps<{
  modelValue: boolean;
}>();

const emit = defineEmits<{
  (e: "update:modelValue", value: boolean): void;
  (
    e: "create",
    payload: { title: string; notes?: string; dueAt?: string },
  ): void;
}>();

const title = ref("");
const notes = ref("");
const dueAt = ref("");

const open = computed({
  get: () => props.modelValue,
  set: (value: boolean) => emit("update:modelValue", value),
});

watch(open, (isOpen) => {
  if (!isOpen) {
    title.value = "";
    notes.value = "";
    dueAt.value = "";
  }
});

function submit() {
  const trimmed = title.value.trim();
  if (!trimmed) return;
  emit("create", {
    title: trimmed,
    notes: notes.value.trim() || undefined,
    dueAt: dueAt.value || undefined,
  });
  open.value = false;
}
</script>
