import { apiClient } from "./client";
import type { AuthResponse, Board, Comment, TaskItem } from "../types";

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

  removeMember: (boardId: number, userId: number) =>
    apiClient.delete<Board>(`/boards/${boardId}/members/${userId}`).then((r) => r.data),

  leaveBoard: (boardId: number) =>
    apiClient.post<Board>(`/boards/${boardId}/leave`).then((r) => r.data),

  deleteBoard: (boardId: number) => apiClient.delete(`/boards/${boardId}`),
};

export const tasksApi = {
  getTasks: (boardId: number) =>
    apiClient.get<TaskItem[]>(`/boards/${boardId}/tasks`).then((r) => r.data),

  createTask: (boardId: number, title: string, description?: string) =>
    apiClient
      .post<TaskItem>(`/boards/${boardId}/tasks`, { title, description })
      .then((r) => r.data),

  // Was missing entirely — backend has had PUT /tasks/{taskId} since the start,
  // but nothing on the frontend ever called it, so there was no way to edit a
  // task's title/description/assignee once created.
  updateTask: (
    boardId: number,
    taskId: number,
    title: string,
    description: string | null,
    assignedUserId: number | null
  ) =>
    apiClient
      .put<TaskItem>(`/boards/${boardId}/tasks/${taskId}`, {
        title,
        description,
        assignedUserId,
      })
      .then((r) => r.data),

  moveTask: (boardId: number, taskId: number, status: string, position: number) =>
    apiClient
      .patch<TaskItem>(`/boards/${boardId}/tasks/${taskId}/move`, { status, position })
      .then((r) => r.data),

  deleteTask: (boardId: number, taskId: number) =>
    apiClient.delete(`/boards/${boardId}/tasks/${taskId}`),
};

export const commentsApi = {
  getComments: (boardId: number, taskId: number) =>
    apiClient
      .get<Comment[]>(`/boards/${boardId}/tasks/${taskId}/comments`)
      .then((r) => r.data),

  createComment: (boardId: number, taskId: number, text: string) =>
    apiClient
      .post<Comment>(`/boards/${boardId}/tasks/${taskId}/comments`, { text })
      .then((r) => r.data),

  deleteComment: (boardId: number, taskId: number, commentId: number) =>
    apiClient.delete(`/boards/${boardId}/tasks/${taskId}/comments/${commentId}`),
};
