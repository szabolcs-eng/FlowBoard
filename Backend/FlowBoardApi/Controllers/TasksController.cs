using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FlowBoardApi.DTOs;
using FlowBoardApi.Extensions;
using FlowBoardApi.Services;

namespace FlowBoardApi.Controllers;

[ApiController]
[Route("api/boards/{boardId}/tasks")]
[Authorize]
public class TasksController : ControllerBase
{
    private readonly ITaskService _taskService;
    private readonly IBoardService _boardService;

    public TasksController(ITaskService taskService, IBoardService boardService)
    {
        _taskService = taskService;
        _boardService = boardService;
    }

    [HttpGet]
    public async Task<ActionResult<List<TaskResponse>>> GetTasks(int boardId)
    {
        if (!await _boardService.IsMemberAsync(boardId, User.GetUserId())) return Forbid();
        return Ok(await _taskService.GetTasksForBoardAsync(boardId));
    }

    [HttpPost]
    public async Task<ActionResult<TaskResponse>> CreateTask(int boardId, CreateTaskRequest request)
    {
        if (!await _boardService.IsMemberAsync(boardId, User.GetUserId())) return Forbid();
        var task = await _taskService.CreateTaskAsync(boardId, request);
        return Ok(task);
    }

    [HttpPut("{taskId}")]
    public async Task<ActionResult<TaskResponse>> UpdateTask(int boardId, int taskId, UpdateTaskRequest request)
    {
        if (!await _boardService.IsMemberAsync(boardId, User.GetUserId())) return Forbid();
        var task = await _taskService.UpdateTaskAsync(boardId, taskId, request);
        if (task is null) return NotFound();
        return Ok(task);
    }

    // Called on every drag-and-drop drop event from the frontend.
    [HttpPatch("{taskId}/move")]
    public async Task<ActionResult<TaskResponse>> MoveTask(int boardId, int taskId, MoveTaskRequest request)
    {
        if (!await _boardService.IsMemberAsync(boardId, User.GetUserId())) return Forbid();
        var task = await _taskService.MoveTaskAsync(boardId, taskId, request);
        if (task is null) return NotFound();
        return Ok(task);
    }

    [HttpDelete("{taskId}")]
    public async Task<IActionResult> DeleteTask(int boardId, int taskId)
    {
        if (!await _boardService.IsMemberAsync(boardId, User.GetUserId())) return Forbid();
        var deleted = await _taskService.DeleteTaskAsync(boardId, taskId);
        if (!deleted) return NotFound();
        return NoContent();
    }
}
