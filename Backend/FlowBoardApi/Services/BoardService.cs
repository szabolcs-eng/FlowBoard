using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using FlowBoardApi.Data;
using FlowBoardApi.DTOs;
using FlowBoardApi.Hubs;
using FlowBoardApi.Models;

namespace FlowBoardApi.Services;

public interface IBoardService
{
    Task<List<BoardResponse>> GetBoardsForUserAsync(int userId);
    Task<BoardResponse?> GetBoardAsync(int boardId, int userId);
    Task<BoardResponse> CreateBoardAsync(CreateBoardRequest request, int ownerId);
    Task<bool> IsMemberAsync(int boardId, int userId);
    Task<BoardResponse?> AddMemberAsync(int boardId, AddMemberRequest request, int requestingUserId);
    Task<BoardResponse?> RemoveMemberAsync(int boardId, int targetUserId, int requestingUserId);
    Task<BoardResponse?> LeaveBoardAsync(int boardId, int userId);
    Task<bool> DeleteBoardAsync(int boardId, int userId);
}

public class BoardService : IBoardService
{
    private readonly AppDbContext _db;
    private readonly IHubContext<BoardHub> _hub;

    public BoardService(AppDbContext db, IHubContext<BoardHub> hub)
    {
        _db = db;
        _hub = hub;
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

        var saved = await ReloadWithMembers(board.Id);
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

        var requester = board.Members.FirstOrDefault(m => m.UserId == requestingUserId);
        if (requester is null || requester.Role != BoardRole.Owner) return null;

        var userToAdd = await _db.Users.SingleOrDefaultAsync(u => u.Email == request.Email);
        if (userToAdd is null) return null;

        if (!board.Members.Any(m => m.UserId == userToAdd.Id))
        {
            board.Members.Add(new BoardMember { UserId = userToAdd.Id, Role = BoardRole.Contributor });
            await _db.SaveChangesAsync();
        }

        return await BroadcastAndReturn(boardId);
    }

    // Only the owner can remove someone else. The owner itself can never be removed
    // this way — a board always needs exactly one owner, so removing them would leave
    // the board ownerless. (An owner who wants out has to delete the board instead —
    // see DeleteBoardAsync — since transferring ownership isn't a feature here.)
    public async Task<BoardResponse?> RemoveMemberAsync(int boardId, int targetUserId, int requestingUserId)
    {
        var board = await _db.Boards
            .Include(b => b.Members)
            .FirstOrDefaultAsync(b => b.Id == boardId);
        if (board is null) return null;

        var requester = board.Members.FirstOrDefault(m => m.UserId == requestingUserId);
        if (requester is null || requester.Role != BoardRole.Owner) return null;

        var target = board.Members.FirstOrDefault(m => m.UserId == targetUserId);
        if (target is null || target.Role == BoardRole.Owner) return null;

        _db.BoardMembers.Remove(target);
        await _db.SaveChangesAsync();

        return await BroadcastAndReturn(boardId);
    }

    // A Contributor can remove themselves at will. The owner cannot — same reasoning
    // as RemoveMemberAsync above.
    public async Task<BoardResponse?> LeaveBoardAsync(int boardId, int userId)
    {
        var board = await _db.Boards
            .Include(b => b.Members)
            .FirstOrDefaultAsync(b => b.Id == boardId);
        if (board is null) return null;

        var membership = board.Members.FirstOrDefault(m => m.UserId == userId);
        if (membership is null || membership.Role == BoardRole.Owner) return null;

        _db.BoardMembers.Remove(membership);
        await _db.SaveChangesAsync();

        return await BroadcastAndReturn(boardId);
    }

    public async Task<bool> DeleteBoardAsync(int boardId, int userId)
    {
        var board = await _db.Boards
            .Include(b => b.Members)
            .FirstOrDefaultAsync(b => b.Id == boardId);
        if (board is null) return false;

        var requester = board.Members.FirstOrDefault(m => m.UserId == userId);
        if (requester is null || requester.Role != BoardRole.Owner) return false;

        // Cascades to Tasks (and their Comments) and BoardMembers via the FK delete
        // behavior configured in AppDbContext — one Remove() takes the whole tree.
        _db.Boards.Remove(board);
        await _db.SaveChangesAsync();

        await _hub.Clients.Group(BoardHub.GroupName(boardId)).SendAsync("BoardDeleted", new { boardId });
        return true;
    }

    private async Task<Board> ReloadWithMembers(int boardId) =>
        await _db.Boards
            .Include(b => b.Members).ThenInclude(m => m.User)
            .FirstAsync(b => b.Id == boardId);

    private async Task<BoardResponse> BroadcastAndReturn(int boardId)
    {
        var board = await ReloadWithMembers(boardId);
        var response = ToResponse(board);
        // Every membership change is broadcast, not just returned to the caller —
        // otherwise a member being added or removed by the owner would only be
        // visible to the owner until everyone else refreshed the page, which would
        // undercut the whole "everything here is live" premise of the app.
        await _hub.Clients.Group(BoardHub.GroupName(boardId)).SendAsync("BoardUpdated", response);
        return response;
    }

    private static BoardResponse ToResponse(Board board) => new(
        board.Id,
        board.Name,
        board.OwnerId,
        board.CreatedAt,
        board.Members.Select(m => new BoardMemberResponse(m.UserId, m.User.DisplayName, m.User.Email, m.Role.ToString())).ToList()
    );
}
