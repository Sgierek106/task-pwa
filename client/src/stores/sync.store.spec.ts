import { beforeEach, describe, expect, it, vi } from "vitest";
import { createPinia, setActivePinia } from "pinia";

const { syncServiceMock, metaRepoMock, loadTasksMock } = vi.hoisted(() => ({
  syncServiceMock: {
    sync: vi.fn(),
  },
  metaRepoMock: {
    getLastSyncAt: vi.fn(),
  },
  loadTasksMock: vi.fn(),
}));

vi.mock("@/sync/syncService", () => ({
  syncService: syncServiceMock,
}));

vi.mock("@/data/metaRepo", () => ({
  metaRepo: metaRepoMock,
}));

vi.mock("@/stores/tasks.store", () => ({
  useTasksStore: () => ({
    loadTasks: loadTasksMock,
  }),
}));

import { useSyncStore } from "@/stores/sync.store";

describe("sync store", () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    syncServiceMock.sync.mockReset();
    metaRepoMock.getLastSyncAt.mockReset();
    loadTasksMock.mockReset();
  });

  it("initializes lastSyncAt from meta repo", async () => {
    metaRepoMock.getLastSyncAt.mockResolvedValue("2026-03-14T12:00:00.000Z");
    const store = useSyncStore();

    await store.initialize();

    expect(store.lastSyncAt).toBe("2026-03-14T12:00:00.000Z");
  });

  it("sync success clears error and reloads tasks", async () => {
    const store = useSyncStore();
    syncServiceMock.sync.mockResolvedValue({
      success: true,
      appliedCount: 2,
      changedCount: 1,
    });
    metaRepoMock.getLastSyncAt.mockResolvedValue("2026-03-14T12:01:00.000Z");

    const result = await store.sync();

    expect(result.success).toBe(true);
    expect(store.lastError).toBeNull();
    expect(loadTasksMock).toHaveBeenCalledTimes(1);
    expect(store.lastSyncAt).toBe("2026-03-14T12:01:00.000Z");
  });

  it("sync failure sets lastError", async () => {
    const store = useSyncStore();
    syncServiceMock.sync.mockResolvedValue({
      success: false,
      appliedCount: 0,
      changedCount: 0,
      error: "boom",
    });

    const result = await store.sync();

    expect(result.success).toBe(false);
    expect(store.lastError).toBe("boom");
    expect(loadTasksMock).not.toHaveBeenCalled();
  });
});
