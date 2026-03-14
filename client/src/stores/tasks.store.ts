import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { tasksRepo } from '@/data/tasksRepo'
import { outboxRepo } from '@/data/outboxRepo'
import type { Task } from '@/data/db'

export type { Task }

export const useTasksStore = defineStore('tasks', () => {
  const tasks = ref<Task[]>([])
  const loading = ref(false)

  const activeTasks = computed(() =>
    tasks.value.filter((t) => !t.deletedAt && !t.isCompleted)
  )

  const completedTasks = computed(() =>
    tasks.value.filter((t) => !t.deletedAt && t.isCompleted)
  )

  async function loadTasks() {
    loading.value = true
    try {
      const all = await tasksRepo.getAll()
      tasks.value = all
    } finally {
      loading.value = false
    }
  }

  async function createTask(title: string, notes?: string, dueAt?: string): Promise<Task> {
    const now = new Date().toISOString()
    const task: Task = {
      id: crypto.randomUUID(),
      title,
      notes: notes ?? null,
      isCompleted: false,
      dueAt: dueAt ?? null,
      updatedAt: now,
      deletedAt: null,
      version: 0,
    }

    await tasksRepo.upsert(task)

    await outboxRepo.add({
      opId: crypto.randomUUID(),
      type: 'upsert',
      entityId: task.id,
      createdAt: now,
      status: 'pending',
      task: {
        id: task.id,
        title: task.title,
        notes: task.notes,
        isCompleted: task.isCompleted,
        dueAt: task.dueAt,
        updatedAt: task.updatedAt,
        deletedAt: task.deletedAt,
      },
    })

    tasks.value.push(task)
    return task
  }

  async function updateTask(id: string, changes: Partial<Omit<Task, 'id' | 'version'>>): Promise<void> {
    const existing = tasks.value.find((t) => t.id === id)
    if (!existing) return

    const now = new Date().toISOString()
    const updated: Task = { ...existing, ...changes, updatedAt: now }

    await tasksRepo.upsert(updated)

    await outboxRepo.add({
      opId: crypto.randomUUID(),
      type: 'upsert',
      entityId: id,
      createdAt: now,
      status: 'pending',
      task: {
        id: updated.id,
        title: updated.title,
        notes: updated.notes,
        isCompleted: updated.isCompleted,
        dueAt: updated.dueAt,
        updatedAt: updated.updatedAt,
        deletedAt: updated.deletedAt,
      },
    })

    const idx = tasks.value.findIndex((t) => t.id === id)
    if (idx !== -1) tasks.value[idx] = updated
  }

  async function deleteTask(id: string): Promise<void> {
    const now = new Date().toISOString()

    await updateTask(id, { deletedAt: now })

    await outboxRepo.add({
      opId: crypto.randomUUID(),
      type: 'delete',
      entityId: id,
      createdAt: now,
      status: 'pending',
    })
  }

  async function toggleComplete(id: string): Promise<void> {
    const task = tasks.value.find((t) => t.id === id)
    if (!task) return
    await updateTask(id, { isCompleted: !task.isCompleted })
  }

  function refreshFromIdb(updatedTasks: Task[]) {
    for (const t of updatedTasks) {
      const idx = tasks.value.findIndex((x) => x.id === t.id)
      if (idx !== -1) {
        tasks.value[idx] = t
      } else {
        tasks.value.push(t)
      }
    }
  }

  return {
    tasks,
    loading,
    activeTasks,
    completedTasks,
    loadTasks,
    createTask,
    updateTask,
    deleteTask,
    toggleComplete,
    refreshFromIdb,
  }
})
