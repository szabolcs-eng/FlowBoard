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

public class CommentServiceTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static (IHubContext<BoardHub> hub, Mock<IClientProxy> proxy) CreateMockHub()
    {
        var proxy = new Mock<IClientProxy>();
        var clients = new Mock<IHubClients>();
        clients.Setup(c => c.Group(It.IsAny<string>())).Returns(proxy.Object);

        var hub = new Mock<IHubContext<BoardHub>>();
        hub.Setup(h => h.Clients).Returns(clients.Object);

        return (hub.Object, proxy);
    }

    [Fact]
    public async Task CreateCommentAsync_PersistsCommentAndBroadcastsCommentAdded()
    {
        await using var db = CreateContext();
        var user = new User { Id = 1, Email = "a@test.com", DisplayName = "Alice" };
        var board = new Board { Id = 1, Name = "Board", OwnerId = 1 };
        var task = new TaskItem { Id = 1, BoardId = 1, Title = "Task" };
        db.Users.Add(user);
        db.Boards.Add(board);
        db.TaskItems.Add(task);
        await db.SaveChangesAsync();

        var (hub, proxy) = CreateMockHub();
        var service = new CommentService(db, hub);

        var result = await service.CreateCommentAsync(board.Id, task.Id, new CreateCommentRequest("Looks good"), user.Id);

        Assert.NotNull(result);
        Assert.Equal("Alice", result!.UserName);
        proxy.Verify(p => p.SendCoreAsync("CommentAdded", It.IsAny<object[]>(), default), Times.Once);
    }

    [Fact]
    public async Task CreateCommentAsync_WhenTaskDoesNotBelongToBoard_ReturnsNull()
    {
        await using var db = CreateContext();
        var task = new TaskItem { Id = 1, BoardId = 999, Title = "Task" };
        db.TaskItems.Add(task);
        await db.SaveChangesAsync();

        var (hub, _) = CreateMockHub();
        var service = new CommentService(db, hub);

        var result = await service.CreateCommentAsync(1, task.Id, new CreateCommentRequest("Text"), userId: 1);

        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteCommentAsync_WhenRequesterIsNotTheAuthor_ReturnsFalseAndDoesNotDelete()
    {
        await using var db = CreateContext();
        var author = new User { Id = 1, Email = "author@test.com", DisplayName = "Author" };
        var otherUser = new User { Id = 2, Email = "other@test.com", DisplayName = "Other" };
        var board = new Board { Id = 1, Name = "Board", OwnerId = 1 };
        var task = new TaskItem { Id = 1, BoardId = 1, Title = "Task" };
        var comment = new Comment { Id = 1, TaskItemId = 1, UserId = author.Id, Text = "Mine" };
        db.Users.AddRange(author, otherUser);
        db.Boards.Add(board);
        db.TaskItems.Add(task);
        db.Comments.Add(comment);
        await db.SaveChangesAsync();

        var (hub, _) = CreateMockHub();
        var service = new CommentService(db, hub);

        var deleted = await service.DeleteCommentAsync(board.Id, task.Id, comment.Id, otherUser.Id);

        Assert.False(deleted);
        Assert.Equal(1, await db.Comments.CountAsync());
    }
}
