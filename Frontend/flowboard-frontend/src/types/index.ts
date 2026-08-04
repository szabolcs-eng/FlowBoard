// These mirror the backend DTOs 1:1 (see Backend/FlowBoardApi/DTOs) so the frontend
// and backend stay in sync — if you rename a field on one side, TypeScript will
// catch the mismatch on the other as soon as you update these.

export type BoardTaskStatus = "Todo" | "InProgress" | "Done";

export interface AuthResponse {
  id: number;
  token: string;
  email: string;
  displayName: string;
}

export interface BoardMember {
  userId: number;
  displayName: string;
  email: string;
  role: string;
}

export interface Board {
  id: number;
  name: string;
  ownerId: number;
  createdAt: string;
  members: BoardMember[];
}

export interface TaskItem {
  id: number;
  boardId: number;
  title: string;
  description: string | null;
  status: BoardTaskStatus;
  position: number;
  assignedUserId: number | null;
  assignedUserName: string | null;
  createdAt: string;
}

export interface Comment {
  id: number;
  taskItemId: number;
  userId: number;
  userName: string;
  text: string;
  createdAt: string;
}

export interface PresenceViewer {
  connectionId: string;
  displayName: string;
}
