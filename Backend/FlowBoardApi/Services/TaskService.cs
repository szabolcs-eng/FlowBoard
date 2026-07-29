using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using FlowBoardApi.Data;
using FlowBoardApi.DTOs;
using FlowBoardApi.Hubs;
using FlowBoardApi.Models;

namespace FlowBoardApi.Services;

public interface ITaskService
{
    Task<List<TaskResponse>> GetTasksForBoardAsync(int boardId);
    Task<TaskResponse?> CreateTaskAsync(int boardId, CreateTaskRequest request);
    Task<TaskResponse?> UpdateTaskAsync(int boardId, int taskId, UpdateTaskRequest request);
    Task<TaskResponse?> MoveTaskAsync(int boardId, int taskId, MoveTaskRequest request);
    Task<bool> DeleteTaskAsync(int boardId, int taskId);
}

// Every mutating method here does two things: persist via EF Core, then broadcast the
// resulting state over the BoardHub group so every connected client updates instantly.
// Keeping the broadcast inside the service (not the controller) means it happens
// consistently no matter which endpoint triggers the change.
public class TaskService : ITaskService
{
    private readonly AppDbContext _db;
    private readonly IHubContext<BoardHub> _hub;

    public TaskService(AppDbContext db, IHubContext<BoardHub> hub)
    {
        _db = db;
        _hub = hub;
    }

    public async Task<List<TaskResponse>> GetTasksForBoardAsync(int boardId)
    {
        var tasks = await _db.TaskItems
            .Include(t => t.AssignedUser)
            .Where(t => t.BoardId == boardId)
            .OrderBy(t => t.Position)
            .ToListAsync();

        return tasks.Select(ToResponse).ToList();
    }

    public async Task<TaskResponse?> CreateTaskAsync(int boardId, CreateTaskRequest request)
    {
        var maxPosition = await _db.TaskItems
            .Where(t => t.BoardId == boardId && t.Status == BoardTaskStatus.Todo)
            .Select(t => (int?)t.Position)
            .MaxAsync() ?? -1;

        var task = new TaskItem
        {
            BoardId = boardId,
            Title = request.Title,
            Description = request.Description,
            Status = BoardTaskStatus.Todo,
            Position = maxPosition + 1
        };

        _db.TaskItems.Add(task);
        await _db.SaveChangesAsync();

        var response = ToResponse(task);
        await _hub.Clients.Group(BoardHub.GroupName(boardId)).SendAsync("TaskCreated", response);
        return response;
    }

    public async Task<TaskResponse?> UpdateTaskAsync(int boardId, int taskId, UpdateTaskRequest request)
    {
        var task = await _db.TaskItems.Include(t => t.AssignedUser)
            .FirstOrDefaultAsync(t => t.Id == taskId && t.BoardId == boardId);
        if (task is null) return null;

        task.Title = request.Title;
        task.Description = request.Description;
        task.AssignedUserId = request.AssignedUserId;
        await _db.SaveChangesAsync();

        // Reload assigned user navigation property in case it changed.
        await _db.Entry(task).Reference(t => t.AssignedUser).LoadAsync();

        var response = ToResponse(task);
        await _hub.Clients.Group(BoardHub.GroupName(boardId)).SendAsync("TaskUpdated", response);
        return response;
    }

    public async Task<TaskResponse?> MoveTaskAsync(int boardId, int taskId, MoveTaskRequest request)
    {
        var task = await _db.TaskItems.Include(t => t.AssignedUser)
            .FirstOrDefaultAsync(t => t.Id == taskId && t.BoardId == boardId);
        if (task is null) return null;

        task.Status = Enum.Parse<BoardTaskStatus>(request.Status);
        task.Position = request.Position;
        await _db.SaveChangesAsync();

        var response = ToResponse(task);
        // A dedicated "TaskMoved" event (vs reusing TaskUpdated) lets the frontend
        // apply a lightweight reorder instead of re-rendering the whole card.
        await _hub.Clients.Group(BoardHub.GroupName(boardId)).SendAsync("TaskMoved", response);
        return response;
    }

    public async Task<bool> DeleteTaskAsync(int boardId, int taskId)
    {
        var task = await _db.TaskItems.FirstOrDefaultAsync(t => t.Id == taskId && t.BoardId == boardId);
        if (task is null) return false;

        _db.TaskItems.Remove(task);
        await _db.SaveChangesAsync();

        await _hub.Clients.Group(BoardHub.GroupName(boardId)).SendAsync("TaskDeleted", taskId);
        return true;
    }

    private static TaskResponse ToResponse(TaskItem task) => new(
        task.Id,
        task.BoardId,
        task.Title,
        task.Description,
        task.Status.ToString(),
        task.Position,
        task.AssignedUserId,
        task.AssignedUser?.DisplayName,
        task.CreatedAt
    );
}
