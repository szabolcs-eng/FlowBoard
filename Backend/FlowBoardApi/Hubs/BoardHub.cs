using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;

namespace FlowBoardApi.Hubs;

// One "group" per board — clients only receive events for boards they've joined.
// The controllers (not this hub) are what actually change data; this hub is purely
// for broadcasting those changes out and for lightweight presence (who's viewing).
[Authorize]
public class BoardHub : Hub
{
    public async Task JoinBoard(int boardId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(boardId));

        var displayName = Context.User?.Identity?.Name ?? "Someone";
        await Clients.OthersInGroup(GroupName(boardId))
            .SendAsync("UserJoined", new { connectionId = Context.ConnectionId, displayName });
    }

    public async Task LeaveBoard(int boardId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(boardId));
        await Clients.OthersInGroup(GroupName(boardId))
            .SendAsync("UserLeft", new { connectionId = Context.ConnectionId });
    }

    public static string GroupName(int boardId) => $"board-{boardId}";
}
