namespace FlowBoardApi.Models;

public class Board
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int OwnerId { get; set; }
    public User Owner { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<BoardMember> Members { get; set; } = new List<BoardMember>();
    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
}

public enum BoardRole
{
    Contributor = 0,
    Owner = 1
}

// Join entity: which users belong to which boards, and their role on that specific board.
public class BoardMember
{
    public int Id { get; set; }
    public int BoardId { get; set; }
    public Board Board { get; set; } = null!;
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public BoardRole Role { get; set; } = BoardRole.Contributor;
}
