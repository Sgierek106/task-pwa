// IndexedDB database setup for task_pwa

export interface Task {
  id: string
  title: string
  notes: string | null
  isCompleted: boolean
  dueAt: string | null
  updatedAt: string
  deletedAt: string | null
  version: number
}

export interface OutboxOp {
  opId: string
  type: 'upsert' | 'delete'
  entityId: string
  createdAt: string
  status: 'pending' | 'sent' | 'error'
  task?: Omit<Task, 'version'>
}

const DB_NAME = 'task_pwa'
const DB_VERSION = 1

let _db: IDBDatabase | null = null

export function openDb(): Promise<IDBDatabase> {
  if (_db) return Promise.resolve(_db)

  return new Promise((resolve, reject) => {
    const req = indexedDB.open(DB_NAME, DB_VERSION)

    req.onupgradeneeded = (event) => {
      const db = (event.target as IDBOpenDBRequest).result

      if (!db.objectStoreNames.contains('tasks')) {
        const tasks = db.createObjectStore('tasks', { keyPath: 'id' })
        tasks.createIndex('updatedAt', 'updatedAt')
        tasks.createIndex('deletedAt', 'deletedAt')
        tasks.createIndex('isCompleted', 'isCompleted')
        tasks.createIndex('dueAt', 'dueAt')
      }

      if (!db.objectStoreNames.contains('outbox')) {
        const outbox = db.createObjectStore('outbox', { keyPath: 'opId' })
        outbox.createIndex('createdAt', 'createdAt')
        outbox.createIndex('type', 'type')
        outbox.createIndex('entityId', 'entityId')
        outbox.createIndex('status', 'status')
      }

      if (!db.objectStoreNames.contains('meta')) {
        db.createObjectStore('meta', { keyPath: 'key' })
      }
    }

    req.onsuccess = () => {
      _db = req.result
      resolve(_db)
    }

    req.onerror = () => reject(req.error)
  })
}

export function idbTransaction(
  db: IDBDatabase,
  storeNames: string | string[],
  mode: IDBTransactionMode = 'readonly'
): IDBTransaction {
  return db.transaction(storeNames, mode)
}

export function idbRequest<T>(req: IDBRequest<T>): Promise<T> {
  return new Promise((resolve, reject) => {
    req.onsuccess = () => resolve(req.result)
    req.onerror = () => reject(req.error)
  })
}
