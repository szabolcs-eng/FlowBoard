import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { boardsApi } from "../api/endpoints";
import type { Board } from "../types";

export default function DashboardPage() {
  const [boards, setBoards] = useState<Board[]>([]);
  const [newBoardName, setNewBoardName] = useState("");

  useEffect(() => {
    boardsApi.getMyBoards().then(setBoards).catch(console.error);
  }, []);

  async function handleCreate(e: React.FormEvent) {
    e.preventDefault();
    if (!newBoardName.trim()) return;
    const board = await boardsApi.createBoard(newBoardName);
    setBoards((prev) => [...prev, board]);
    setNewBoardName("");
  }

  return (
    <div className="p-6 max-w-2xl mx-auto">
      <h1 className="text-2xl font-bold mb-4">Your Boards</h1>

      <form onSubmit={handleCreate} className="flex gap-2 mb-6">
        <input
          value={newBoardName}
          onChange={(e) => setNewBoardName(e.target.value)}
          placeholder="New board name"
          className="border rounded px-3 py-2 flex-1"
        />
        <button type="submit" className="bg-blue-600 text-white rounded px-4 py-2">
          Create
        </button>
      </form>

      <ul className="flex flex-col gap-2">
        {boards.map((board) => (
          <li key={board.id}>
            <Link
              to={`/boards/${board.id}`}
              className="block bg-white border rounded p-3 hover:bg-gray-50"
            >
              {board.name}
              <span className="text-xs text-gray-500 ml-2">
                {board.members.length} member{board.members.length !== 1 ? "s" : ""}
              </span>
            </Link>
          </li>
        ))}
      </ul>
    </div>
  );
}
