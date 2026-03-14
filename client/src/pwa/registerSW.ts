import { useRegisterSW } from 'virtual:pwa-register/vue'

export function usePwaUpdate() {
  const { needRefresh, updateServiceWorker } = useRegisterSW({
    onRegistered(r: ServiceWorkerRegistration | undefined) {
      console.log('[PWA] Service Worker registered:', r)
    },
    onRegisterError(error: unknown) {
      console.error('[PWA] Service Worker registration error:', error)
    },
  })

  function update() {
    updateServiceWorker(true)
  }

  return { needRefresh, update }
}
