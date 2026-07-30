import { useEffect, useState } from "react";
import { useParams } from "react-router-dom";
import { tasksApi } from "../api/endpoints";
import { useBoardConnection, subscribeToTaskEvents } from "../hooks/useBoardConnection";
import type { TaskItem, BoardTaskStatus } from "../types";

const COLUMNS: BoardTaskStatus[] = ["Todo", "InProgress", "Done"];

export default function BoardPage() {
  const { boardId } = useParams<{ boardId: string }>();
  const id = Number(boardId);

  const [tasks, setTasks] = useState<TaskItem[]>([]);
  const [newTaskTitle, setNewTaskTitle] = useState("");
  const { isConnected, connection } = useBoardConnection(id);

  // 1. Initial load over plain REST — SignalR is for *changes*, not bulk fetch.
  useEffect(() => {
    tasksApi.getTasks(id).then(setTasks).catch(console.error);
  }, [id]);

  // 2. Once connected, patch local state as events arrive from other clients (or ourselves).
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
    // No optimistic add here — the TaskCreated SignalR event (which the server sends back
    // to us too, not just other clients) is what actually adds it to local state. The
    // onCreated guard above prevents a duplicate if this REST response and the SignalR
    // event both resolve.
    await tasksApi.createTask(id, title);
  }

  async function handleDrop(taskId: number, newStatus: BoardTaskStatus, newPosition: number) {
    // Optimistic local update, then confirm with the server. On success the server also
    // broadcasts TaskMoved back to us over SignalR — onMoved above will just re-apply the
    // same state, which is a harmless no-op.
    setTasks((prev) =>
      prev.map((t) => (t.id === taskId ? { ...t, status: newStatus, position: newPosition } : t))
    );
    await tasksApi.moveTask(id, taskId, newStatus, newPosition);
  }

  return (
    <div className="p-6">
      <div className="flex items-center gap-2 mb-4">
        <h1 className="text-2xl font-bold">Board #{id}</h1>
        <span
          className={`text-xs px-2 py-1 rounded-full ${
            isConnected ? "bg-green-100 text-green-700" : "bg-gray-100 text-gray-500"
          }`}
        >
          {isConnected ? "Live" : "Connecting..."}
        </span>
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
                <div
                  key={task.id}
                  draggable
                  onDragStart={(e) => e.dataTransfer.setData("taskId", String(task.id))}
                  className="bg-white rounded shadow-sm p-3 mb-2 cursor-grab"
                >
                  <p className="font-medium">{task.title}</p>
                  {task.assignedUserName && (
                    <p className="text-xs text-gray-500 mt-1">{task.assignedUserName}</p>
                  )}
                </div>
              ))}
          </div>
        ))}
      </div>
    </div>
  );
}
