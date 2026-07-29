# 🗂️ FlowBoard — Real-Time Collaborative Task Board

A multi-user Kanban board where task moves, edits, and comments sync **instantly** across every connected client — no refresh, no polling. Built to demonstrate real-time state synchronization on top of a clean, tested full-stack architecture.

## ✨ Key Features

- 🔐 **Secure Authentication** — JWT-based sessions with BCrypt password hashing
- ⚡ **Real-Time Sync** — SignalR broadcasts task creates/updates/moves to every board member live
- 🧑‍🤝‍🧑 **Boards & Roles** — Board owners invite members; global roles (Admin/Manager/Member) and per-board roles (Owner/Contributor)
- 🖱️ **Drag-and-Drop** — Move tasks between Todo / In Progress / Done, synced live to everyone viewing the board
- 🛡️ **Validated & Tested** — FluentValidation on every request DTO; xUnit + Moq backend tests, Vitest frontend tests
- 🤖 **CI Pipeline** — GitHub Actions builds and tests both backend and frontend on every push

## 🛠️ Tech Stack

**Backend:** C# / ASP.NET Core (.NET 10) · Entity Framework Core (Code-First) · SQLite · SignalR · JWT + BCrypt · FluentValidation · xUnit + Moq

**Frontend:** React + TypeScript (Vite) · Tailwind CSS v4 · React Router · Axios (with JWT interceptor) · @microsoft/signalr · Vitest + Testing Library

## 🏗️ Architecture

```
Backend/FlowBoardApi/       ASP.NET Core Web API (Controllers, Services, DTOs, EF Core, SignalR hub)
Backend/FlowBoardApi.Tests/ xUnit test project
Frontend/flowboard-frontend/  React + TypeScript SPA
```

The service layer owns both persistence (EF Core) and broadcasting (SignalR) for every mutation — so a task move triggered from any client is saved to SQLite *and* pushed live to every other client on that board in one place, not scattered across controllers.

## 🚀 Getting Started

### Backend
```bash
cd Backend
dotnet restore
cd FlowBoardApi
dotnet user-secrets init
dotnet user-secrets set "Jwt:Key" "a-long-random-development-secret-min-32-chars"
dotnet ef database update   # or just run — Program.cs auto-migrates on startup
dotnet run
```
API available at `https://localhost:7000`, Swagger UI at `/swagger`.

### Frontend
```bash
cd Frontend/flowboard-frontend
npm install
cp .env.example .env
npm run dev
```
App available at `http://localhost:5173`.

### Tests
```bash
# Backend
cd Backend && dotnet test

# Frontend
cd Frontend/flowboard-frontend && npm run test
```

## 📸 Screenshots

_Add screenshots/GIF here once the UI is built — a short clip of two browser windows updating live is the single most convincing thing you can put in this README._

## 📄 License

MIT
