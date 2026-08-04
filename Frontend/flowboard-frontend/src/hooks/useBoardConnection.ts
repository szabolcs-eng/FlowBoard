import { useEffect, useRef, useState } from "react";
import * as signalR from "@microsoft/signalr";
import type { TaskItem, Comment, PresenceViewer } from "../types";
import { getToken } from "../lib/auth";

const HUB_BASE_URL = import.meta.env.VITE_HUB_BASE_URL ?? "https://localhost:7000/hubs/board";

interface UseBoardConnectionResult {
  isConnected: boolean;
  // Consumers subscribe to these via useEffect in the component that owns task state
  // (see BoardPage) rather than this hook owning task state itself — keeps this hook
  // reusable and focused purely on the connection lifecycle.
  connection: signalR.HubConnection | null;
}

export function useBoardConnection(boardId: number): UseBoardConnectionResult {
  const [isConnected, setIsConnected] = useState(false);
  const connectionRef = useRef<signalR.HubConnection | null>(null);

  useEffect(() => {
    const token = getToken();
    if (!token) return;

    const connection = new signalR.HubConnectionBuilder()
      .withUrl(HUB_BASE_URL, { accessTokenFactory: () => token })
      .withAutomaticReconnect()
      .build();

    connectionRef.current = connection;

    connection
      .start()
      .then(() => {
        setIsConnected(true);
        return connection.invoke("JoinBoard", boardId);
      })
      .catch((err) => console.error("SignalR connection failed:", err));

    connection.onreconnected(() => {
      setIsConnected(true);
      connection.invoke("JoinBoard", boardId).catch(console.error);
    });
    connection.onclose(() => setIsConnected(false));

    return () => {
      connection.invoke("LeaveBoard", boardId).catch(() => {});
      connection.stop();
    };
  }, [boardId]);

  return { isConnected, connection: connectionRef.current };
}

// Small typed wrapper so components don't sprinkle string event names everywhere.
export function subscribeToTaskEvents(
  connection: signalR.HubConnection,
  handlers: {
    onCreated: (task: TaskItem) => void;
    onUpdated: (task: TaskItem) => void;
    onMoved: (task: TaskItem) => void;
    onDeleted: (taskId: number) => void;
  }
) {
  connection.on("TaskCreated", handlers.onCreated);
  connection.on("TaskUpdated", handlers.onUpdated);
  connection.on("TaskMoved", handlers.onMoved);
  connection.on("TaskDeleted", handlers.onDeleted);

  return () => {
    connection.off("TaskCreated", handlers.onCreated);
    connection.off("TaskUpdated", handlers.onUpdated);
    connection.off("TaskMoved", handlers.onMoved);
    connection.off("TaskDeleted", handlers.onDeleted);
  };
}

export function subscribeToCommentEvents(
  connection: signalR.HubConnection,
  handlers: {
    onAdded: (comment: Comment) => void;
    onDeleted: (payload: { taskId: number; commentId: number }) => void;
  }
) {
  connection.on("CommentAdded", handlers.onAdded);
  connection.on("CommentDeleted", handlers.onDeleted);

  return () => {
    connection.off("CommentAdded", handlers.onAdded);
    connection.off("CommentDeleted", handlers.onDeleted);
  };
}

export function subscribeToPresenceEvents(
  connection: signalR.HubConnection,
  handlers: {
    // Sent once, right after JoinBoard resolves — the full list of who's already
    // viewing. Everything after that arrives incrementally via onJoined/onLeft.
    onSnapshot: (viewers: PresenceViewer[]) => void;
    onJoined: (viewer: PresenceViewer) => void;
    onLeft: (payload: { connectionId: string }) => void;
  }
) {
  connection.on("PresenceSnapshot", handlers.onSnapshot);
  connection.on("UserJoined", handlers.onJoined);
  connection.on("UserLeft", handlers.onLeft);

  return () => {
    connection.off("PresenceSnapshot", handlers.onSnapshot);
    connection.off("UserJoined", handlers.onJoined);
    connection.off("UserLeft", handlers.onLeft);
  };
}
