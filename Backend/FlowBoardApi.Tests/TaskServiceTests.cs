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

public class TaskServiceTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    // IHubContext<BoardHub>.Clients.Group(...).SendAsync(...) is three chained interfaces.
    // SendAsync itself is an extension method that calls SendCoreAsync under the hood, so
    // that's the method we actually mock.
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
    public async Task CreateTaskAsync_PersistsTaskAndBroadcastsTaskCreated()
    {
        await using var db = CreateContext();
        var board = new Board { Id = 1, Name = "Board", OwnerId = 1 };
        db.Boards.Add(board);
        await db.SaveChangesAsync();

        var (hub, proxy) = CreateMockHub();
        var service = new TaskService(db, hub);

        var result = await service.CreateTaskAsync(board.Id, new CreateTaskRequest("Write tests", null));

        Assert.Equal("Write tests", result!.Title);
        Assert.Equal("Todo", result.Status);

        proxy.Verify(p => p.SendCoreAsync(
            "TaskCreated",
            It.Is<object[]>(args => args.Length == 1),
            default), Times.Once);
    }

    [Fact]
    public async Task MoveTaskAsync_UpdatesStatusAndPosition_AndBroadcastsTaskMoved()
    {
        await using var db = CreateContext();
        var board = new Board { Id = 1, Name = "Board", OwnerId = 1 };
        var task = new TaskItem { Id = 1, BoardId = 1, Title = "Task", Status = BoardTaskStatus.Todo, Position = 0 };
        db.Boards.Add(board);
        db.TaskItems.Add(task);
        await db.SaveChangesAsync();

        var (hub, proxy) = CreateMockHub();
        var service = new TaskService(db, hub);

        var result = await service.MoveTaskAsync(board.Id, task.Id, new MoveTaskRequest("InProgress", 2));

        Assert.Equal("InProgress", result!.Status);
        Assert.Equal(2, result.Position);
        proxy.Verify(p => p.SendCoreAsync("TaskMoved", It.IsAny<object[]>(), default), Times.Once);
    }

    [Fact]
    public async Task MoveTaskAsync_WhenTaskDoesNotBelongToBoard_ReturnsNull()
    {
        await using var db = CreateContext();
        var task = new TaskItem { Id = 1, BoardId = 999, Title = "Task", Status = BoardTaskStatus.Todo };
        db.TaskItems.Add(task);
        await db.SaveChangesAsync();

        var (hub, _) = CreateMockHub();
        var service = new TaskService(db, hub);

        // boardId = 1 doesn't match the task's actual BoardId (999).
        var result = await service.MoveTaskAsync(1, task.Id, new MoveTaskRequest("Done", 0));

        Assert.Null(result);
    }
}
