import { useEffect, useState } from "react";
import { useParams } from "react-router-dom";
import { boardsApi, tasksApi } from "../api/endpoints";
import { useBoardConnection, subscribeToTaskEvents } from "../hooks/useBoardConnection";
import type { Board, TaskItem, BoardTaskStatus } from "../types";

const COLUMNS: { status: BoardTaskStatus; label: string }[] = [
  { status: "Todo", label: "To do" },
  { status: "InProgress", label: "In progress" },
  { status: "Done", label: "Done" },
];

export default function BoardPage() {
  const { boardId } = useParams<{ boardId: string }>();
  const id = Number(boardId);

  const [board, setBoard] = useState<Board | null>(null);
  const [tasks, setTasks] = useState<TaskItem[]>([]);
  const [newTaskTitle, setNewTaskTitle] = useState("");
  const [memberEmail, setMemberEmail] = useState("");
  const [memberError, setMemberError] = useState<string | null>(null);
  const { isConnected, connection } = useBoardConnection(id);

  useEffect(() => {
    boardsApi.getBoard(id).then(setBoard).catch(console.error);
  }, [id]);

  useEffect(() => {
    tasksApi.getTasks(id).then(setTasks).catch(console.error);
  }, [id]);

  useEffect(() => {
    if (!connection) return;

    return subscribeToTaskEvents(connection, {
      onCreated: (task) =>
        setTasks((prev) => (prev.some((t) => t.id === task.id) ? prev : [...prev, task])),
      onUpdated: (task) => setTasks((prev) => prev.map((t) => (t.id === task.id ? task : t))),
      onMoved: (task) => setTasks((prev) => prev.map((t) => (t.id === task.id ? task : t))),
      onDeleted: (taskId) => setTasks((prev) => prev.filter((t) => t.id !== taskId)),
    });
  }, [connection]);

  async function handleCreateTask(e: React.FormEvent) {
    e.preventDefault();
    if (!newTaskTitle.trim()) return;
    const title = newTaskTitle;
    setNewTaskTitle("");
    await tasksApi.createTask(id, title);
  }

  async function handleDrop(taskId: number, newStatus: BoardTaskStatus, newPosition: number) {
    setTasks((prev) =>
      prev.map((t) => (t.id === taskId ? { ...t, status: newStatus, position: newPosition } : t))
    );
    await tasksApi.moveTask(id, taskId, newStatus, newPosition);
  }

  async function handleAddMember(e: React.FormEvent) {
    e.preventDefault();
    setMemberError(null);
    try {
      const updatedBoard = await boardsApi.addMember(id, memberEmail);
      setBoard(updatedBoard);
      setMemberEmail("");
    } catch {
      setMemberError(
        "Couldn't add that member — only the board owner can add members, and the email must belong to a registered user."
      );
    }
  }

  if (!board) {
    return <p className="text-sm text-muted">Loading board…</p>;
  }

  return (
    <div>
      <div className="mb-1 flex items-center gap-2">
        <h1 className="font-[family-name:var(--font-display)] text-2xl font-semibold text-ink">
          {board.name}
        </h1>
        <span
          className={`inline-flex items-center gap-1.5 rounded-full px-2.5 py-0.5 text-xs font-medium ${
            isConnected ? "bg-live-soft text-live" : "bg-paper text-muted"
          }`}
        >
          {isConnected && <span className="live-dot h-1.5 w-1.5 rounded-full bg-live" />}
          {isConnected ? "Live" : "Connecting…"}
        </span>
      </div>

      <div className="mb-6">
        <div className="mb-2 flex flex-wrap gap-2">
          {board.members.map((m) => (
            <span
              key={m.userId}
              className="rounded-full border border-border bg-surface px-2.5 py-1 text-xs text-muted"
            >
              {m.displayName} · {m.role}
            </span>
          ))}
        </div>
        <form onSubmit={handleAddMember} className="flex max-w-sm gap-2">
          <input
            type="email"
            value={memberEmail}
            onChange={(e) => setMemberEmail(e.target.value)}
            placeholder="Add member by email"
            className="flex-1 rounded-md border border-border bg-surface px-2.5 py-1.5 text-sm text-ink placeholder:text-muted focus:border-brand"
          />
          <button
            type="submit"
            className="rounded-md border border-border px-3 py-1.5 text-sm font-medium text-ink transition-colors hover:border-brand hover:text-brand"
          >
            Add
          </button>
        </form>
        {memberError && <p className="mt-1 text-xs text-danger">{memberError}</p>}
      </div>

      <form onSubmit={handleCreateTask} className="mb-6 flex max-w-md gap-2">
        <input
          value={newTaskTitle}
          onChange={(e) => setNewTaskTitle(e.target.value)}
          placeholder="New task title"
          className="flex-1 rounded-md border border-border bg-surface px-3 py-2 text-sm text-ink placeholder:text-muted focus:border-brand"
        />
        <button
          type="submit"
          className="rounded-md bg-brand px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-brand-hover"
        >
          Add task
        </button>
      </form>

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
        {COLUMNS.map(({ status, label }) => (
          <div
            key={status}
            className="min-h-[300px] rounded-xl border border-border bg-surface/60 p-3"
            onDragOver={(e) => e.preventDefault()}
            onDrop={(e) => {
              const taskId = Number(e.dataTransfer.getData("taskId"));
              const columnTasks = tasks.filter((t) => t.status === status);
              handleDrop(taskId, status, columnTasks.length);
            }}
          >
            <h2 className="mb-3 text-xs font-semibold uppercase tracking-wide text-muted">
              {label}
            </h2>
            {tasks
              .filter((t) => t.status === status)
              .sort((a, b) => a.position - b.position)
              .map((task) => (
                <TaskCard key={task.id} boardId={id} task={task} members={board.members} />
              ))}
          </div>
        ))}
      </div>
    </div>
  );
}

function TaskCard({
  boardId,
  task,
  members,
}: {
  boardId: number;
  task: TaskItem;
  members: { userId: number; displayName: string }[];
}) {
  const [isEditing, setIsEditing] = useState(false);
  const [title, setTitle] = useState(task.title);
  const [description, setDescription] = useState(task.description ?? "");
  const [assignedUserId, setAssignedUserId] = useState<number | null>(task.assignedUserId);

  async function handleSave(e: React.FormEvent) {
    e.preventDefault();
    await tasksApi.updateTask(boardId, task.id, title, description || null, assignedUserId);
    setIsEditing(false);
  }

  async function handleDelete() {
    if (!confirm(`Delete "${task.title}"?`)) return;
    await tasksApi.deleteTask(boardId, task.id);
  }

  if (isEditing) {
    return (
      <form
        onSubmit={handleSave}
        className="mb-2 flex flex-col gap-2 rounded-lg border border-brand bg-surface p-3 shadow-sm"
      >
        <input
          value={title}
          onChange={(e) => setTitle(e.target.value)}
          className="rounded-md border border-border px-2 py-1 text-sm focus:border-brand"
          required
        />
        <textarea
          value={description}
          onChange={(e) => setDescription(e.target.value)}
          placeholder="Description"
          className="rounded-md border border-border px-2 py-1 text-sm focus:border-brand"
          rows={2}
        />
        <select
          value={assignedUserId ?? ""}
          onChange={(e) => setAssignedUserId(e.target.value ? Number(e.target.value) : null)}
          className="rounded-md border border-border px-2 py-1 text-sm focus:border-brand"
        >
          <option value="">Unassigned</option>
          {members.map((m) => (
            <option key={m.userId} value={m.userId}>
              {m.displayName}
            </option>
          ))}
        </select>
        <div className="flex gap-2">
          <button
            type="submit"
            className="rounded-md bg-brand px-2.5 py-1 text-xs font-medium text-white hover:bg-brand-hover"
          >
            Save
          </button>
          <button
            type="button"
            onClick={() => setIsEditing(false)}
            className="rounded-md border border-border px-2.5 py-1 text-xs font-medium text-ink hover:bg-paper"
          >
            Cancel
          </button>
        </div>
      </form>
    );
  }

  return (
    <div
      draggable
      onDragStart={(e) => e.dataTransfer.setData("taskId", String(task.id))}
      className="group mb-2 cursor-grab rounded-lg border border-border bg-surface p-3 shadow-sm transition-shadow hover:shadow-md"
    >
      <div className="flex items-start justify-between gap-2">
        <p className="text-sm font-medium text-ink">{task.title}</p>
        <div className="flex shrink-0 gap-1 opacity-0 transition-opacity group-hover:opacity-100">
          <button
            onClick={() => setIsEditing(true)}
            className="text-xs text-muted hover:text-brand"
            aria-label="Edit task"
          >
            ✎
          </button>
          <button
            onClick={handleDelete}
            className="text-xs text-muted hover:text-danger"
            aria-label="Delete task"
          >
            ✕
          </button>
        </div>
      </div>
      {task.description && <p className="mt-1 text-xs text-muted">{task.description}</p>}
      {task.assignedUserName && (
        <p className="mt-2 inline-block rounded-full bg-brand-soft px-2 py-0.5 text-xs text-brand">
          {task.assignedUserName}
        </p>
      )}
    </div>
  );
}
