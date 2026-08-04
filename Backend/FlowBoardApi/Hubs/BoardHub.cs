using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;

namespace FlowBoardApi.Hubs;

// One "group" per board — clients only receive events for boards they've joined.
// The controllers (not this hub) are what actually change data; this hub is purely
// for broadcasting those changes out and for presence (who's currently viewing).
[Authorize]
public class BoardHub : Hub
{
    // Tracks which board each live connection is on, purely in-memory. This is what
    // lets OnDisconnectedAsync know which group to notify when a connection drops
    // without an explicit LeaveBoard call (closed tab, lost network, etc.), and lets
    // a newly-joining client ask "who's already here" instead of only learning about
    // people who join *after* them.
    //
    // Known limitation: this is process-local. If FlowBoardApi ever ran as more than
    // one server instance, presence would need to move to a shared backplane (e.g.
    // Redis) instead of a static dictionary — fine for a single-instance deployment,
    // not fine at real scale.
    private static readonly ConcurrentDictionary<string, (int BoardId, string DisplayName)> Presence = new();

    public async Task JoinBoard(int boardId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(boardId));

        var displayName = Context.User?.Identity?.Name ?? "Someone";
        Presence[Context.ConnectionId] = (boardId, displayName);

        // Tell the new joiner who's already here...
        var existingViewers = Presence
            .Where(kvp => kvp.Value.BoardId == boardId && kvp.Key != Context.ConnectionId)
            .Select(kvp => new { connectionId = kvp.Key, displayName = kvp.Value.DisplayName })
            .ToList();
        await Clients.Caller.SendAsync("PresenceSnapshot", existingViewers);

        // ...and tell everyone already here that a new viewer arrived.
        await Clients.OthersInGroup(GroupName(boardId))
            .SendAsync("UserJoined", new { connectionId = Context.ConnectionId, displayName });
    }

    public async Task LeaveBoard(int boardId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(boardId));
        Presence.TryRemove(Context.ConnectionId, out _);
        await Clients.OthersInGroup(GroupName(boardId))
            .SendAsync("UserLeft", new { connectionId = Context.ConnectionId });
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // Covers the case LeaveBoard doesn't: a closed tab or dropped connection never
        // calls LeaveBoard explicitly, so without this, other viewers would see a
        // "ghost" presence that never goes away.
        if (Presence.TryRemove(Context.ConnectionId, out var info))
        {
            await Clients.OthersInGroup(GroupName(info.BoardId))
                .SendAsync("UserLeft", new { connectionId = Context.ConnectionId });
        }

        await base.OnDisconnectedAsync(exception);
    }

    public static string GroupName(int boardId) => $"board-{boardId}";
}
