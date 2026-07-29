import axios from "axios";

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? "https://localhost:7000/api";

export const apiClient = axios.create({
  baseURL: API_BASE_URL,
});

// Automatically attach the JWT to every outgoing request, so individual
// components/hooks never have to think about auth headers.
apiClient.interceptors.request.use((config) => {
  const token = localStorage.getItem("flowboard_token");
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// If the token expires or is invalid, the API returns 401 — bounce to login
// instead of leaving the user staring at a broken screen.
apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      localStorage.removeItem("flowboard_token");
      window.location.href = "/login";
    }
    return Promise.reject(error);
  }
);
