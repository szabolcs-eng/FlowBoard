using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using Moq;
using FlowBoardApi.Data;
using FlowBoardApi.DTOs;
using FlowBoardApi.Hubs;
using FlowBoardApi.Models;
using FlowBoardApi.Services;
using Xunit;

namespace FlowBoardApi.Tests;

public class BoardServiceTests
{
    private static AppDbContext CreateContext()
    {
        // A fresh, isolated in-memory DB per test — no shared state, no need for a real SQLite file.
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static IHubContext<BoardHub> CreateMockHub()
    {
        var proxy = new Mock<IClientProxy>();
        var clients = new Mock<IHubClients>();
        clients.Setup(c => c.Group(It.IsAny<string>())).Returns(proxy.Object);

        var hub = new Mock<IHubContext<BoardHub>>();
        hub.Setup(h => h.Clients).Returns(clients.Object);
        return hub.Object;
    }

    [Fact]
    public async Task CreateBoardAsync_AddsOwnerAsBoardMemberWithOwnerRole()
    {
        await using var db = CreateContext();
        var owner = new User { Email = "owner@test.com", DisplayName = "Owner" };
        db.Users.Add(owner);
        await db.SaveChangesAsync();

        var service = new BoardService(db, CreateMockHub());
        var result = await service.CreateBoardAsync(new CreateBoardRequest("Sprint Board"), owner.Id);

        Assert.Single(result.Members);
        Assert.Equal("Owner", result.Members[0].Role);
        Assert.Equal(owner.Id, result.OwnerId);
    }

    [Fact]
    public async Task AddMemberAsync_WhenRequesterIsNotOwner_ReturnsNull()
    {
        await using var db = CreateContext();
        var owner = new User { Id = 1, Email = "owner@test.com", DisplayName = "Owner" };
        var contributor = new User { Id = 2, Email = "member@test.com", DisplayName = "Member" };
        var newUser = new User { Id = 3, Email = "new@test.com", DisplayName = "New" };
        db.Users.AddRange(owner, contributor, newUser);

        var board = new Board { Id = 1, Name = "Board", OwnerId = owner.Id };
        board.Members.Add(new BoardMember { UserId = owner.Id, Role = BoardRole.Owner });
        board.Members.Add(new BoardMember { UserId = contributor.Id, Role = BoardRole.Contributor });
        db.Boards.Add(board);
        await db.SaveChangesAsync();

        var service = new BoardService(db, CreateMockHub());
        var result = await service.AddMemberAsync(board.Id, new AddMemberRequest(newUser.Email), contributor.Id);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetBoardAsync_WhenUserIsNotAMember_ReturnsNull()
    {
        await using var db = CreateContext();
        var owner = new User { Id = 1, Email = "owner@test.com", DisplayName = "Owner" };
        var outsider = new User { Id = 2, Email = "outsider@test.com", DisplayName = "Outsider" };
        db.Users.AddRange(owner, outsider);

        var board = new Board { Id = 1, Name = "Private Board", OwnerId = owner.Id };
        board.Members.Add(new BoardMember { UserId = owner.Id, Role = BoardRole.Owner });
        db.Boards.Add(board);
        await db.SaveChangesAsync();

        var service = new BoardService(db, CreateMockHub());
        var result = await service.GetBoardAsync(board.Id, outsider.Id);

        Assert.Null(result);
    }

    [Fact]
    public async Task RemoveMemberAsync_WhenRequesterIsOwner_RemovesContributor()
    {
        await using var db = CreateContext();
        var owner = new User { Id = 1, Email = "owner@test.com", DisplayName = "Owner" };
        var contributor = new User { Id = 2, Email = "member@test.com", DisplayName = "Member" };
        db.Users.AddRange(owner, contributor);

        var board = new Board { Id = 1, Name = "Board", OwnerId = owner.Id };
        board.Members.Add(new BoardMember { UserId = owner.Id, Role = BoardRole.Owner });
        board.Members.Add(new BoardMember { UserId = contributor.Id, Role = BoardRole.Contributor });
        db.Boards.Add(board);
        await db.SaveChangesAsync();

        var service = new BoardService(db, CreateMockHub());
        var result = await service.RemoveMemberAsync(board.Id, contributor.Id, owner.Id);

        Assert.NotNull(result);
        Assert.Single(result!.Members);
        Assert.DoesNotContain(result.Members, m => m.UserId == contributor.Id);
    }

    [Fact]
    public async Task RemoveMemberAsync_CannotRemoveTheOwner()
    {
        await using var db = CreateContext();
        var owner = new User { Id = 1, Email = "owner@test.com", DisplayName = "Owner" };
        var contributor = new User { Id = 2, Email = "member@test.com", DisplayName = "Member" };
        db.Users.AddRange(owner, contributor);

        var board = new Board { Id = 1, Name = "Board", OwnerId = owner.Id };
        board.Members.Add(new BoardMember { UserId = owner.Id, Role = BoardRole.Owner });
        board.Members.Add(new BoardMember { UserId = contributor.Id, Role = BoardRole.Contributor });
        db.Boards.Add(board);
        await db.SaveChangesAsync();

        var service = new BoardService(db, CreateMockHub());
        // A contributor cannot remove the owner, and — since only the owner has the
        // authority to remove anyone in the first place — this also can't happen the
        // other way around (owner "removing" themselves) because that path is explicitly
        // blocked below regardless of who calls it.
        var result = await service.RemoveMemberAsync(board.Id, owner.Id, owner.Id);

        Assert.Null(result);
    }

    [Fact]
    public async Task LeaveBoardAsync_OwnerCannotLeave()
    {
        await using var db = CreateContext();
        var owner = new User { Id = 1, Email = "owner@test.com", DisplayName = "Owner" };
        db.Users.Add(owner);

        var board = new Board { Id = 1, Name = "Board", OwnerId = owner.Id };
        board.Members.Add(new BoardMember { UserId = owner.Id, Role = BoardRole.Owner });
        db.Boards.Add(board);
        await db.SaveChangesAsync();

        var service = new BoardService(db, CreateMockHub());
        var result = await service.LeaveBoardAsync(board.Id, owner.Id);

        Assert.Null(result);
        Assert.Equal(1, await db.BoardMembers.CountAsync());
    }

    [Fact]
    public async Task LeaveBoardAsync_ContributorCanLeave()
    {
        await using var db = CreateContext();
        var owner = new User { Id = 1, Email = "owner@test.com", DisplayName = "Owner" };
        var contributor = new User { Id = 2, Email = "member@test.com", DisplayName = "Member" };
        db.Users.AddRange(owner, contributor);

        var board = new Board { Id = 1, Name = "Board", OwnerId = owner.Id };
        board.Members.Add(new BoardMember { UserId = owner.Id, Role = BoardRole.Owner });
        board.Members.Add(new BoardMember { UserId = contributor.Id, Role = BoardRole.Contributor });
        db.Boards.Add(board);
        await db.SaveChangesAsync();

        var service = new BoardService(db, CreateMockHub());
        var result = await service.LeaveBoardAsync(board.Id, contributor.Id);

        Assert.NotNull(result);
        Assert.DoesNotContain(result!.Members, m => m.UserId == contributor.Id);
    }

    [Fact]
    public async Task DeleteBoardAsync_WhenRequesterIsNotOwner_ReturnsFalseAndDoesNotDelete()
    {
        await using var db = CreateContext();
        var owner = new User { Id = 1, Email = "owner@test.com", DisplayName = "Owner" };
        var contributor = new User { Id = 2, Email = "member@test.com", DisplayName = "Member" };
        db.Users.AddRange(owner, contributor);

        var board = new Board { Id = 1, Name = "Board", OwnerId = owner.Id };
        board.Members.Add(new BoardMember { UserId = owner.Id, Role = BoardRole.Owner });
        board.Members.Add(new BoardMember { UserId = contributor.Id, Role = BoardRole.Contributor });
        db.Boards.Add(board);
        await db.SaveChangesAsync();

        var service = new BoardService(db, CreateMockHub());
        var deleted = await service.DeleteBoardAsync(board.Id, contributor.Id);

        Assert.False(deleted);
        Assert.Equal(1, await db.Boards.CountAsync());
    }

    [Fact]
    public async Task DeleteBoardAsync_WhenOwner_CascadesTasksAndComments()
    {
        await using var db = CreateContext();
        var owner = new User { Id = 1, Email = "owner@test.com", DisplayName = "Owner" };
        db.Users.Add(owner);

        var board = new Board { Id = 1, Name = "Board", OwnerId = owner.Id };
        board.Members.Add(new BoardMember { UserId = owner.Id, Role = BoardRole.Owner });
        db.Boards.Add(board);

        var task = new TaskItem { Id = 1, BoardId = 1, Title = "Task" };
        db.TaskItems.Add(task);
        db.Comments.Add(new Comment { Id = 1, TaskItemId = 1, UserId = owner.Id, Text = "Hi" });
        await db.SaveChangesAsync();

        var service = new BoardService(db, CreateMockHub());
        var deleted = await service.DeleteBoardAsync(board.Id, owner.Id);

        Assert.True(deleted);
        Assert.Equal(0, await db.Boards.CountAsync());
        Assert.Equal(0, await db.TaskItems.CountAsync());
        Assert.Equal(0, await db.Comments.CountAsync());
        Assert.Equal(0, await db.BoardMembers.CountAsync());
    }
}
