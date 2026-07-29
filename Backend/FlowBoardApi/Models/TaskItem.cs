namespace FlowBoardApi.Models;

// Named BoardTaskStatus (not TaskStatus) to avoid colliding with System.Threading.Tasks.TaskStatus,
// which ImplicitUsings pulls into scope automatically.
public enum BoardTaskStatus
{
    Todo = 0,
    InProgress = 1,
    Done = 2
}

public class TaskItem
{
    public int Id { get; set; }
    public int BoardId { get; set; }
    public Board Board { get; set; } = null!;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public BoardTaskStatus Status { get; set; } = BoardTaskStatus.Todo;
    // Position lets us preserve manual ordering within a column (drag-and-drop reordering).
    public int Position { get; set; }
    public int? AssignedUserId { get; set; }
    public User? AssignedUser { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
}

public class Comment
{
    public int Id { get; set; }
    public int TaskItemId { get; set; }
    public TaskItem TaskItem { get; set; } = null!;
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public string Text { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
