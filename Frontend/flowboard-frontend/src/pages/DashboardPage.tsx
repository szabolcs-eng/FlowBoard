import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { boardsApi } from "../api/endpoints";
import type { Board } from "../types";

export default function DashboardPage() {
  const [boards, setBoards] = useState<Board[]>([]);
  const [loaded, setLoaded] = useState(false);
  const [newBoardName, setNewBoardName] = useState("");

  useEffect(() => {
    boardsApi
      .getMyBoards()
      .then(setBoards)
      .catch(console.error)
      .finally(() => setLoaded(true));
  }, []);

  async function handleCreate(e: React.FormEvent) {
    e.preventDefault();
    if (!newBoardName.trim()) return;
    const board = await boardsApi.createBoard(newBoardName);
    setBoards((prev) => [...prev, board]);
    setNewBoardName("");
  }

  return (
    <div>
      <h1 className="mb-1 font-[family-name:var(--font-display)] text-2xl font-semibold text-ink">
        Your boards
      </h1>
      <p className="mb-6 text-sm text-muted">
        Everything moves live here — teammates see your changes the instant you make them.
      </p>

      <form onSubmit={handleCreate} className="mb-8 flex gap-2 max-w-md">
        <input
          value={newBoardName}
          onChange={(e) => setNewBoardName(e.target.value)}
          placeholder="Name a new board"
          className="flex-1 rounded-md border border-border bg-surface px-3 py-2 text-sm text-ink placeholder:text-muted focus:border-brand"
        />
        <button
          type="submit"
          className="rounded-md bg-brand px-4 py-2 text-sm font-medium text-white transition-colors hover:bg-brand-hover"
        >
          Create board
        </button>
      </form>

      {loaded && boards.length === 0 ? (
        <div className="rounded-xl border border-dashed border-border bg-surface px-6 py-12 text-center">
          <p className="font-[family-name:var(--font-display)] text-lg font-semibold text-ink">
            No boards yet
          </p>
          <p className="mx-auto mt-1 max-w-sm text-sm text-muted">
            Create your first board above, then open it and invite a teammate — you'll see their
            moves land in real time.
          </p>
        </div>
      ) : (
        <ul className="flex flex-col gap-2">
          {boards.map((board) => (
            <li key={board.id}>
              <Link
                to={`/boards/${board.id}`}
                className="flex items-center justify-between rounded-lg border border-border bg-surface px-4 py-3 transition-colors hover:border-brand"
              >
                <span className="font-medium text-ink">{board.name}</span>
                <span className="text-xs text-muted">
                  {board.members.length} member{board.members.length !== 1 ? "s" : ""}
                </span>
              </Link>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
