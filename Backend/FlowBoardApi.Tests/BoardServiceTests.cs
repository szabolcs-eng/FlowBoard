using Microsoft.EntityFrameworkCore;
using FlowBoardApi.Data;
using FlowBoardApi.DTOs;
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

    [Fact]
    public async Task CreateBoardAsync_AddsOwnerAsBoardMemberWithOwnerRole()
    {
        await using var db = CreateContext();
        var owner = new User { Email = "owner@test.com", DisplayName = "Owner" };
        db.Users.Add(owner);
        await db.SaveChangesAsync();

        var service = new BoardService(db);
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

        var service = new BoardService(db);
        // contributor (not owner) tries to add a new member — should be rejected.
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

        var service = new BoardService(db);
        var result = await service.GetBoardAsync(board.Id, outsider.Id);

        Assert.Null(result);
    }
}
