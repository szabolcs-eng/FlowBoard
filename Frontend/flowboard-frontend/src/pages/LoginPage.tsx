import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { authApi } from "../api/endpoints";
import { setAuth } from "../lib/auth";
import BrandMark from "../components/BrandMark";

export default function LoginPage() {
  const [mode, setMode] = useState<"login" | "register">("login");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [displayName, setDisplayName] = useState("");
  const [error, setError] = useState<string | null>(null);
  const navigate = useNavigate();

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    try {
      const result =
        mode === "login"
          ? await authApi.login(email, password)
          : await authApi.register(email, password, displayName);
      setAuth(result.token, { id: result.id, email: result.email, displayName: result.displayName });
      navigate("/");
    } catch {
      setError(
        mode === "login"
          ? "That email or password doesn't match an account."
          : "Couldn't create the account — email may already be in use, or the password needs to be at least 8 characters."
      );
    }
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-paper px-4">
      <div className="w-full max-w-sm">
        <div className="mb-8 flex justify-center">
          <BrandMark />
        </div>

        <div className="rounded-xl border border-border bg-surface p-8 shadow-sm">
          <h1 className="mb-1 font-[family-name:var(--font-display)] text-xl font-semibold text-ink">
            {mode === "login" ? "Welcome back" : "Create your account"}
          </h1>
          <p className="mb-6 text-sm text-muted">
            {mode === "login"
              ? "Log in to see your boards."
              : "Set up an account to start a board."}
          </p>

          <form onSubmit={handleSubmit} className="flex flex-col gap-3">
            {mode === "register" && (
              <input
                type="text"
                placeholder="Display name"
                value={displayName}
                onChange={(e) => setDisplayName(e.target.value)}
                className="rounded-md border border-border bg-surface px-3 py-2 text-sm text-ink placeholder:text-muted focus:border-brand"
                required
              />
            )}
            <input
              type="email"
              placeholder="Email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              className="rounded-md border border-border bg-surface px-3 py-2 text-sm text-ink placeholder:text-muted focus:border-brand"
              required
            />
            <input
              type="password"
              placeholder="Password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              className="rounded-md border border-border bg-surface px-3 py-2 text-sm text-ink placeholder:text-muted focus:border-brand"
              required
            />
            {error && (
              <p className="rounded-md bg-danger-soft px-3 py-2 text-sm text-danger">{error}</p>
            )}
            <button
              type="submit"
              className="mt-1 rounded-md bg-brand px-3 py-2 text-sm font-medium text-white transition-colors hover:bg-brand-hover"
            >
              {mode === "login" ? "Log in" : "Create account"}
            </button>
          </form>

          <button
            onClick={() => {
              setMode(mode === "login" ? "register" : "login");
              setError(null);
            }}
            className="mt-5 w-full text-center text-sm text-brand hover:underline"
          >
            {mode === "login" ? "Need an account? Register" : "Already have an account? Log in"}
          </button>
        </div>
      </div>
    </div>
  );
}
