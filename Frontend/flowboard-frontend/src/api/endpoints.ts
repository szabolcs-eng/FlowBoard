import { apiClient } from "./client";
import type { AuthResponse, Board, TaskItem } from "../types";

export const authApi = {
  login: (email: string, password: string) =>
    apiClient.post<AuthResponse>("/auth/login", { email, password }).then((r) => r.data),

  register: (email: string, password: string, displayName: string) =>
    apiClient
      .post<AuthResponse>("/auth/register", { email, password, displayName })
      .then((r) => r.data),
};

export const boardsApi = {
  getMyBoards: () => apiClient.get<Board[]>("/boards").then((r) => r.data),

  getBoard: (id: number) => apiClient.get<Board>(`/boards/${id}`).then((r) => r.data),

  createBoard: (name: string) =>
    apiClient.post<Board>("/boards", { name }).then((r) => r.data),

  addMember: (boardId: number, email: string) =>
    apiClient.post<Board>(`/boards/${boardId}/members`, { email }).then((r) => r.data),
};

export const tasksApi = {
  getTasks: (boardId: number) =>
    apiClient.get<TaskItem[]>(`/boards/${boardId}/tasks`).then((r) => r.data),

  createTask: (boardId: number, title: string, description?: string) =>
    apiClient
      .post<TaskItem>(`/boards/${boardId}/tasks`, { title, description })
      .then((r) => r.data),

  moveTask: (boardId: number, taskId: number, status: string, position: number) =>
    apiClient
      .patch<TaskItem>(`/boards/${boardId}/tasks/${taskId}/move`, { status, position })
      .then((r) => r.data),

  deleteTask: (boardId: number, taskId: number) =>
    apiClient.delete(`/boards/${boardId}/tasks/${taskId}`),
};
