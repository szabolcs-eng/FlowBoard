import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { authApi } from "../api/endpoints";

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
      localStorage.setItem("flowboard_token", result.token);
      navigate("/");
    } catch {
      setError(
        mode === "login"
          ? "Invalid email or password."
          : "Registration failed — email may already be in use, or password must be at least 8 characters."
      );
    }
  }

  return (
    <div className="max-w-sm mx-auto mt-24">
      <h1 className="text-2xl font-bold mb-6">
        {mode === "login" ? "Log in to FlowBoard" : "Create your FlowBoard account"}
      </h1>
      <form onSubmit={handleSubmit} className="flex flex-col gap-3">
        {mode === "register" && (
          <input
            type="text"
            placeholder="Display name"
            value={displayName}
            onChange={(e) => setDisplayName(e.target.value)}
            className="border rounded px-3 py-2"
            required
          />
        )}
        <input
          type="email"
          placeholder="Email"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          className="border rounded px-3 py-2"
          required
        />
        <input
          type="password"
          placeholder="Password"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          className="border rounded px-3 py-2"
          required
        />
        {error && <p className="text-red-600 text-sm">{error}</p>}
        <button type="submit" className="bg-blue-600 text-white rounded px-3 py-2">
          {mode === "login" ? "Log in" : "Register"}
        </button>
      </form>

      <button
        onClick={() => {
          setMode(mode === "login" ? "register" : "login");
          setError(null);
        }}
        className="text-sm text-blue-600 mt-4 underline"
      >
        {mode === "login" ? "Need an account? Register" : "Already have an account? Log in"}
      </button>
    </div>
  );
}
