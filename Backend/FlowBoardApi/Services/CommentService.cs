using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using FlowBoardApi.Data;
using FlowBoardApi.DTOs;
using FlowBoardApi.Hubs;
using FlowBoardApi.Models;

namespace FlowBoardApi.Services;

public interface ICommentService
{
    Task<List<CommentResponse>> GetCommentsForTaskAsync(int boardId, int taskId);
    Task<CommentResponse?> CreateCommentAsync(int boardId, int taskId, CreateCommentRequest request, int userId);
    Task<bool> DeleteCommentAsync(int boardId, int taskId, int commentId, int requestingUserId);
}

// Same shape as TaskService: every mutation persists via EF Core, then broadcasts over
// the same board-scoped SignalR group. Comments are scoped by board group (not a
// separate per-task group) — a task's comment thread is only ever open by people
// already viewing that board, so reusing the existing group avoids managing a second
// set of SignalR groups for no real benefit.
public class CommentService : ICommentService
{
    private readonly AppDbContext _db;
    private readonly IHubContext<BoardHub> _hub;

    public CommentService(AppDbContext db, IHubContext<BoardHub> hub)
    {
        _db = db;
        _hub = hub;
    }

    public async Task<List<CommentResponse>> GetCommentsForTaskAsync(int boardId, int taskId)
    {
        var comments = await _db.Comments
            .Include(c => c.User)
            .Where(c => c.TaskItemId == taskId && c.TaskItem.BoardId == boardId)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync();

        return comments.Select(ToResponse).ToList();
    }

    public async Task<CommentResponse?> CreateCommentAsync(int boardId, int taskId, CreateCommentRequest request, int userId)
    {
        var taskExists = await _db.TaskItems.AnyAsync(t => t.Id == taskId && t.BoardId == boardId);
        if (!taskExists) return null;

        var comment = new Comment
        {
            TaskItemId = taskId,
            UserId = userId,
            Text = request.Text
        };

        _db.Comments.Add(comment);
        await _db.SaveChangesAsync();

        // Reload with the author's display name populated for the response/broadcast.
        await _db.Entry(comment).Reference(c => c.User).LoadAsync();

        var response = ToResponse(comment);
        await _hub.Clients.Group(BoardHub.GroupName(boardId)).SendAsync("CommentAdded", response);
        return response;
    }

    public async Task<bool> DeleteCommentAsync(int boardId, int taskId, int commentId, int requestingUserId)
    {
        var comment = await _db.Comments
            .FirstOrDefaultAsync(c => c.Id == commentId && c.TaskItemId == taskId && c.TaskItem.BoardId == boardId);
        if (comment is null) return false;

        // Only the comment's own author can delete it — unlike task deletion, which any
        // board member can do, a comment is treated as personal speech, not shared board state.
        if (comment.UserId != requestingUserId) return false;

        _db.Comments.Remove(comment);
        await _db.SaveChangesAsync();

        await _hub.Clients.Group(BoardHub.GroupName(boardId))
            .SendAsync("CommentDeleted", new { taskId, commentId });
        return true;
    }

    private static CommentResponse ToResponse(Comment comment) => new(
        comment.Id,
        comment.TaskItemId,
        comment.UserId,
        comment.User.DisplayName,
        comment.Text,
        comment.CreatedAt
    );
}
