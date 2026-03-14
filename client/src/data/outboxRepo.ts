import { openDb, idbRequest, type OutboxOp } from './db'

export const outboxRepo = {
  async add(op: OutboxOp): Promise<void> {
    const db = await openDb()
    const tx = db.transaction('outbox', 'readwrite')
    const store = tx.objectStore('outbox')
    await idbRequest(store.put(op))
  },

  async getPending(): Promise<OutboxOp[]> {
    const db = await openDb()
    const tx = db.transaction('outbox', 'readonly')
    const store = tx.objectStore('outbox')
    const index = store.index('status')
    return idbRequest<OutboxOp[]>(index.getAll('pending'))
  },

  async markSent(opIds: string[]): Promise<void> {
    const db = await openDb()
    const tx = db.transaction('outbox', 'readwrite')
    const store = tx.objectStore('outbox')
    for (const opId of opIds) {
      const op = await idbRequest<OutboxOp | undefined>(store.get(opId))
      if (op) {
        op.status = 'sent'
        store.put(op)
      }
    }
    await new Promise<void>((resolve, reject) => {
      tx.oncomplete = () => resolve()
      tx.onerror = () => reject(tx.error)
    })
  },

  async remove(opIds: string[]): Promise<void> {
    const db = await openDb()
    const tx = db.transaction('outbox', 'readwrite')
    const store = tx.objectStore('outbox')
    for (const opId of opIds) {
      store.delete(opId)
    }
    await new Promise<void>((resolve, reject) => {
      tx.oncomplete = () => resolve()
      tx.onerror = () => reject(tx.error)
    })
  },

  async getAll(): Promise<OutboxOp[]> {
    const db = await openDb()
    const tx = db.transaction('outbox', 'readonly')
    const store = tx.objectStore('outbox')
    return idbRequest<OutboxOp[]>(store.getAll())
  },
}
