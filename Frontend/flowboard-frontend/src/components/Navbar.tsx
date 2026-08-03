import { NavLink, useNavigate } from "react-router-dom";
import BrandMark from "./BrandMark";
import { clearAuth, getStoredUser } from "../lib/auth";

const navLinkClasses = ({ isActive }: { isActive: boolean }) =>
  `text-sm font-medium px-3 py-1.5 rounded-md transition-colors ${
    isActive ? "bg-brand-soft text-brand" : "text-muted hover:text-ink hover:bg-paper"
  }`;

export default function Navbar() {
  const navigate = useNavigate();
  const user = getStoredUser();

  function handleLogout() {
    clearAuth();
    navigate("/login", { replace: true });
  }

  return (
    <header className="sticky top-0 z-10 border-b border-border bg-surface/90 backdrop-blur">
      <div className="mx-auto flex max-w-5xl items-center justify-between px-6 py-3">
        <div className="flex items-center gap-8">
          <NavLink to="/" aria-label="FlowBoard home">
            <BrandMark />
          </NavLink>
          <nav className="flex items-center gap-1">
            <NavLink to="/" end className={navLinkClasses}>
              Boards
            </NavLink>
          </nav>
        </div>

        <div className="flex items-center gap-4">
          {user && (
            <span className="hidden text-sm text-muted sm:inline">
              Signed in as <span className="font-medium text-ink">{user.displayName}</span>
            </span>
          )}
          <button
            onClick={handleLogout}
            className="rounded-md border border-border px-3 py-1.5 text-sm font-medium text-ink transition-colors hover:border-danger hover:text-danger"
          >
            Log out
          </button>
        </div>
      </div>
    </header>
  );
}
