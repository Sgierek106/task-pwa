import { openDb, idbRequest, type Task } from './db'

export type { Task }

export const tasksRepo = {
  async getAll(): Promise<Task[]> {
    const db = await openDb()
    const tx = db.transaction('tasks', 'readonly')
    const store = tx.objectStore('tasks')
    return idbRequest<Task[]>(store.getAll())
  },

  async getById(id: string): Promise<Task | undefined> {
    const db = await openDb()
    const tx = db.transaction('tasks', 'readonly')
    const store = tx.objectStore('tasks')
    return idbRequest<Task | undefined>(store.get(id))
  },

  async upsert(task: Task): Promise<void> {
    const db = await openDb()
    const tx = db.transaction('tasks', 'readwrite')
    const store = tx.objectStore('tasks')
    await idbRequest(store.put(task))
  },

  async upsertMany(tasks: Task[]): Promise<void> {
    const db = await openDb()
    const tx = db.transaction('tasks', 'readwrite')
    const store = tx.objectStore('tasks')
    for (const task of tasks) {
      store.put(task)
    }
    await new Promise<void>((resolve, reject) => {
      tx.oncomplete = () => resolve()
      tx.onerror = () => reject(tx.error)
    })
  },

  async delete(id: string): Promise<void> {
    const db = await openDb()
    const tx = db.transaction('tasks', 'readwrite')
    const store = tx.objectStore('tasks')
    await idbRequest(store.delete(id))
  },
}
