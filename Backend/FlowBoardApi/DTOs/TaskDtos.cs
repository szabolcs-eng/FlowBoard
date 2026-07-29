namespace FlowBoardApi.DTOs;

public record CreateTaskRequest(string Title, string? Description);

public record UpdateTaskRequest(string Title, string? Description, int? AssignedUserId);

// Separate endpoint/DTO for moves — a drag-and-drop move is a distinct, high-frequency
// operation from a full field edit, and keeping it its own DTO keeps the SignalR payload small.
public record MoveTaskRequest(string Status, int Position);

public record TaskResponse(
    int Id,
    int BoardId,
    string Title,
    string? Description,
    string Status,
    int Position,
    int? AssignedUserId,
    string? AssignedUserName,
    DateTime CreatedAt
);

public record CreateCommentRequest(string Text);

public record CommentResponse(int Id, int TaskItemId, int UserId, string UserName, string Text, DateTime CreatedAt);
