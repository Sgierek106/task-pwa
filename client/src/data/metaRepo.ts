import { openDb, idbRequest } from './db'

interface MetaEntry {
  key: string
  value: string
}

async function getMeta(key: string): Promise<string | null> {
  const db = await openDb()
  const tx = db.transaction('meta', 'readonly')
  const store = tx.objectStore('meta')
  const entry = await idbRequest<MetaEntry | undefined>(store.get(key))
  return entry?.value ?? null
}

async function setMeta(key: string, value: string): Promise<void> {
  const db = await openDb()
  const tx = db.transaction('meta', 'readwrite')
  const store = tx.objectStore('meta')
  await idbRequest(store.put({ key, value }))
}

function generateUUID(): string {
  return crypto.randomUUID()
}

export const metaRepo = {
  async getClientId(): Promise<string> {
    let id = await getMeta('clientId')
    if (!id) {
      id = generateUUID()
      await setMeta('clientId', id)
    }
    return id
  },

  async getUserKey(): Promise<string | null> {
    return getMeta('userKey')
  },

  async setUserKey(key: string): Promise<void> {
    return setMeta('userKey', key)
  },

  async getLastSyncAt(): Promise<string | null> {
    return getMeta('lastSyncAt')
  },

  async setLastSyncAt(value: string): Promise<void> {
    return setMeta('lastSyncAt', value)
  },
}
