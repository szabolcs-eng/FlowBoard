import { useEffect, useState } from "react";
import { useParams } from "react-router-dom";
import { boardsApi, tasksApi } from "../api/endpoints";
import { useBoardConnection, subscribeToTaskEvents } from "../hooks/useBoardConnection";
import type { Board, TaskItem, BoardTaskStatus } from "../types";

const COLUMNS: BoardTaskStatus[] = ["Todo", "InProgress", "Done"];

export default function BoardPage() {
  const { boardId } = useParams<{ boardId: string }>();
  const id = Number(boardId);

  const [board, setBoard] = useState<Board | null>(null);
  const [tasks, setTasks] = useState<TaskItem[]>([]);
  const [newTaskTitle, setNewTaskTitle] = useState("");
  const [memberEmail, setMemberEmail] = useState("");
  const [memberError, setMemberError] = useState<string | null>(null);
  const { isConnected, connection } = useBoardConnection(id);

  // Board detail (name + members) — previously never fetched, so the page just
  // showed "Board #id" and there was no way to see or add members.
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
      // Backend returns 403 if you're not the board owner, or the add is a no-op
      // if the email doesn't match a registered user — same message either way,
      // since the API doesn't currently distinguish them in the response body.
      setMemberError("Couldn't add that member — only the board owner can add members, and the email must belong to a registered user.");
    }
  }

  if (!board) {
    return <div className="p-6">Loading board...</div>;
  }

  return (
    <div className="p-6">
      <div className="flex items-center gap-2 mb-1">
        <h1 className="text-2xl font-bold">{board.name}</h1>
        <span
          className={`text-xs px-2 py-1 rounded-full ${
            isConnected ? "bg-green-100 text-green-700" : "bg-gray-100 text-gray-500"
          }`}
        >
          {isConnected ? "Live" : "Connecting..."}
        </span>
      </div>

      <div className="mb-4">
        <div className="flex flex-wrap gap-2 mb-2">
          {board.members.map((m) => (
            <span
              key={m.userId}
              className="text-xs bg-gray-100 rounded-full px-2 py-1 text-gray-600"
            >
              {m.displayName} ({m.role})
            </span>
          ))}
        </div>
        <form onSubmit={handleAddMember} className="flex gap-2 max-w-sm">
          <input
            type="email"
            value={memberEmail}
            onChange={(e) => setMemberEmail(e.target.value)}
            placeholder="Add member by email"
            className="border rounded px-2 py-1 text-sm flex-1"
          />
          <button type="submit" className="text-sm bg-gray-700 text-white rounded px-3 py-1">
            Add
          </button>
        </form>
        {memberError && <p className="text-red-600 text-xs mt-1">{memberError}</p>}
      </div>

      <form onSubmit={handleCreateTask} className="flex gap-2 mb-6 max-w-md">
        <input
          value={newTaskTitle}
          onChange={(e) => setNewTaskTitle(e.target.value)}
          placeholder="New task title"
          className="border rounded px-3 py-2 flex-1"
        />
        <button type="submit" className="bg-blue-600 text-white rounded px-4 py-2">
          Add task
        </button>
      </form>

      <div className="grid grid-cols-3 gap-4">
        {COLUMNS.map((status) => (
          <div
            key={status}
            className="bg-gray-50 rounded-lg p-3 min-h-[300px]"
            onDragOver={(e) => e.preventDefault()}
            onDrop={(e) => {
              const taskId = Number(e.dataTransfer.getData("taskId"));
              const columnTasks = tasks.filter((t) => t.status === status);
              handleDrop(taskId, status, columnTasks.length);
            }}
          >
            <h2 className="font-semibold text-sm text-gray-600 mb-2">{status}</h2>
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

// Handles its own edit/delete state locally — the parent's `tasks` array stays
// the single source of truth (updated via SignalR's TaskUpdated/TaskDeleted
// events once the server confirms the change), this component just triggers
// the API calls and toggles its own "am I in edit mode" flag.
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
        className="bg-white rounded shadow-sm p-3 mb-2 flex flex-col gap-2"
      >
        <input
          value={title}
          onChange={(e) => setTitle(e.target.value)}
          className="border rounded px-2 py-1 text-sm"
          required
        />
        <textarea
          value={description}
          onChange={(e) => setDescription(e.target.value)}
          placeholder="Description"
          className="border rounded px-2 py-1 text-sm"
          rows={2}
        />
        <select
          value={assignedUserId ?? ""}
          onChange={(e) => setAssignedUserId(e.target.value ? Number(e.target.value) : null)}
          className="border rounded px-2 py-1 text-sm"
        >
          <option value="">Unassigned</option>
          {members.map((m) => (
            <option key={m.userId} value={m.userId}>
              {m.displayName}
            </option>
          ))}
        </select>
        <div className="flex gap-2">
          <button type="submit" className="text-xs bg-blue-600 text-white rounded px-2 py-1">
            Save
          </button>
          <button
            type="button"
            onClick={() => setIsEditing(false)}
            className="text-xs bg-gray-200 rounded px-2 py-1"
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
      className="bg-white rounded shadow-sm p-3 mb-2 cursor-grab group"
    >
      <div className="flex justify-between items-start gap-2">
        <p className="font-medium">{task.title}</p>
        <div className="flex gap-1 opacity-0 group-hover:opacity-100 transition-opacity">
          <button
            onClick={() => setIsEditing(true)}
            className="text-xs text-gray-400 hover:text-gray-700"
            aria-label="Edit task"
          >
            ✎
          </button>
          <button
            onClick={handleDelete}
            className="text-xs text-gray-400 hover:text-red-600"
            aria-label="Delete task"
          >
            ✕
          </button>
        </div>
      </div>
      {task.description && <p className="text-xs text-gray-500 mt-1">{task.description}</p>}
      {task.assignedUserName && (
        <p className="text-xs text-gray-500 mt-1">{task.assignedUserName}</p>
      )}
    </div>
  );
}
