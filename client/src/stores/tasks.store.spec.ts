import { beforeEach, describe, expect, it, vi } from "vitest";
import { createPinia, setActivePinia } from "pinia";
import { useTasksStore, type Task } from "@/stores/tasks.store";

const { tasksRepoMock, outboxRepoMock } = vi.hoisted(() => ({
  tasksRepoMock: {
    getAll: vi.fn(),
    upsert: vi.fn(),
  },
  outboxRepoMock: {
    add: vi.fn(),
  },
}));

vi.mock("@/data/tasksRepo", () => ({
  tasksRepo: tasksRepoMock,
}));

vi.mock("@/data/outboxRepo", () => ({
  outboxRepo: outboxRepoMock,
}));

describe("tasks store", () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    vi.useFakeTimers();
    vi.setSystemTime(new Date("2026-03-14T00:00:00.000Z"));

    tasksRepoMock.getAll.mockReset();
    tasksRepoMock.upsert.mockReset();
    outboxRepoMock.add.mockReset();

    vi.spyOn(crypto, "randomUUID")
      .mockReturnValueOnce("11111111-1111-4111-8111-111111111111")
      .mockReturnValueOnce("22222222-2222-4222-8222-222222222222")
      .mockReturnValue("33333333-3333-4333-8333-333333333333");
  });

  it("loads tasks from repository", async () => {
    const store = useTasksStore();
    const expected: Task[] = [
      {
        id: "1",
        title: "Task one",
        notes: null,
        isCompleted: false,
        dueAt: null,
        updatedAt: "2026-01-01T00:00:00.000Z",
        deletedAt: null,
        version: 1,
      },
    ];
    tasksRepoMock.getAll.mockResolvedValue(expected);

    await store.loadTasks();

    expect(store.loading).toBe(false);
    expect(store.tasks).toEqual(expected);
  });

  it("creates a task and queues an upsert operation", async () => {
    const store = useTasksStore();

    await store.createTask("New Task", "notes", "2026-03-30");

    expect(tasksRepoMock.upsert).toHaveBeenCalledTimes(1);
    expect(outboxRepoMock.add).toHaveBeenCalledTimes(1);
    expect(store.tasks).toHaveLength(1);
    expect(store.tasks[0].title).toBe("New Task");
  });

  it("deletes task by tombstoning and adding delete operation", async () => {
    const store = useTasksStore();
    store.tasks.push({
      id: "task-1",
      title: "Task",
      notes: null,
      isCompleted: false,
      dueAt: null,
      updatedAt: "2026-03-14T00:00:00.000Z",
      deletedAt: null,
      version: 0,
    });

    await store.deleteTask("task-1");

    expect(tasksRepoMock.upsert).toHaveBeenCalledTimes(1);
    expect(outboxRepoMock.add).toHaveBeenCalledTimes(2);
    expect(store.tasks[0].deletedAt).not.toBeNull();
  });
});
