// Centralizes what was previously scattered raw localStorage.getItem/setItem calls
// across LoginPage, client.ts, and App.tsx — one place owns the two keys, so adding
// the Navbar's "who's logged in" display doesn't mean guessing the storage shape.

export interface StoredUser {
  id: number;
  email: string;
  displayName: string;
}

const TOKEN_KEY = "flowboard_token";
const USER_KEY = "flowboard_user";

export function setAuth(token: string, user: StoredUser) {
  localStorage.setItem(TOKEN_KEY, token);
  localStorage.setItem(USER_KEY, JSON.stringify(user));
}

export function clearAuth() {
  localStorage.removeItem(TOKEN_KEY);
  localStorage.removeItem(USER_KEY);
}

export function getToken(): string | null {
  return localStorage.getItem(TOKEN_KEY);
}

export function getStoredUser(): StoredUser | null {
  const raw = localStorage.getItem(USER_KEY);
  if (!raw) return null;
  try {
    return JSON.parse(raw) as StoredUser;
  } catch {
    return null;
  }
}

export function isAuthenticated(): boolean {
  return Boolean(getToken());
}
