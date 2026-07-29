using Microsoft.EntityFrameworkCore;
using FlowBoardApi.Data;
using FlowBoardApi.DTOs;
using FlowBoardApi.Models;

namespace FlowBoardApi.Services;

public interface IBoardService
{
    Task<List<BoardResponse>> GetBoardsForUserAsync(int userId);
    Task<BoardResponse?> GetBoardAsync(int boardId, int userId);
    Task<BoardResponse> CreateBoardAsync(CreateBoardRequest request, int ownerId);
    Task<bool> IsMemberAsync(int boardId, int userId);
    Task<BoardResponse?> AddMemberAsync(int boardId, AddMemberRequest request, int requestingUserId);
}

public class BoardService : IBoardService
{
    private readonly AppDbContext _db;

    public BoardService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<BoardResponse>> GetBoardsForUserAsync(int userId)
    {
        var boards = await _db.Boards
            .Include(b => b.Members).ThenInclude(m => m.User)
            .Where(b => b.Members.Any(m => m.UserId == userId))
            .ToListAsync();

        return boards.Select(ToResponse).ToList();
    }

    public async Task<BoardResponse?> GetBoardAsync(int boardId, int userId)
    {
        var board = await _db.Boards
            .Include(b => b.Members).ThenInclude(m => m.User)
            .FirstOrDefaultAsync(b => b.Id == boardId);

        if (board is null || !board.Members.Any(m => m.UserId == userId))
            return null;

        return ToResponse(board);
    }

    public async Task<BoardResponse> CreateBoardAsync(CreateBoardRequest request, int ownerId)
    {
        var board = new Board
        {
            Name = request.Name,
            OwnerId = ownerId
        };
        board.Members.Add(new BoardMember { UserId = ownerId, Role = BoardRole.Owner });

        _db.Boards.Add(board);
        await _db.SaveChangesAsync();

        // Reload with the owner's user info populated for the response.
        var saved = await _db.Boards
            .Include(b => b.Members).ThenInclude(m => m.User)
            .FirstAsync(b => b.Id == board.Id);

        return ToResponse(saved);
    }

    public async Task<bool> IsMemberAsync(int boardId, int userId)
    {
        return await _db.BoardMembers.AnyAsync(m => m.BoardId == boardId && m.UserId == userId);
    }

    public async Task<BoardResponse?> AddMemberAsync(int boardId, AddMemberRequest request, int requestingUserId)
    {
        var board = await _db.Boards
            .Include(b => b.Members).ThenInclude(m => m.User)
            .FirstOrDefaultAsync(b => b.Id == boardId);

        if (board is null) return null;

        // Only the board owner can add members.
        var requester = board.Members.FirstOrDefault(m => m.UserId == requestingUserId);
        if (requester is null || requester.Role != BoardRole.Owner) return null;

        var userToAdd = await _db.Users.SingleOrDefaultAsync(u => u.Email == request.Email);
        if (userToAdd is null) return null;

        if (!board.Members.Any(m => m.UserId == userToAdd.Id))
        {
            board.Members.Add(new BoardMember { UserId = userToAdd.Id, Role = BoardRole.Contributor });
            await _db.SaveChangesAsync();
        }

        return ToResponse(board);
    }

    private static BoardResponse ToResponse(Board board) => new(
        board.Id,
        board.Name,
        board.OwnerId,
        board.CreatedAt,
        board.Members.Select(m => new BoardMemberResponse(m.UserId, m.User.DisplayName, m.User.Email, m.Role.ToString())).ToList()
    );
}
