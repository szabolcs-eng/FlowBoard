using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FlowBoardApi.DTOs;
using FlowBoardApi.Extensions;
using FlowBoardApi.Services;

namespace FlowBoardApi.Controllers;

[ApiController]
[Route("api/boards/{boardId}/tasks/{taskId}/comments")]
[Authorize]
public class CommentsController : ControllerBase
{
    private readonly ICommentService _commentService;
    private readonly IBoardService _boardService;

    public CommentsController(ICommentService commentService, IBoardService boardService)
    {
        _commentService = commentService;
        _boardService = boardService;
    }

    [HttpGet]
    public async Task<ActionResult<List<CommentResponse>>> GetComments(int boardId, int taskId)
    {
        if (!await _boardService.IsMemberAsync(boardId, User.GetUserId())) return Forbid();
        return Ok(await _commentService.GetCommentsForTaskAsync(boardId, taskId));
    }

    [HttpPost]
    public async Task<ActionResult<CommentResponse>> CreateComment(int boardId, int taskId, CreateCommentRequest request)
    {
        if (!await _boardService.IsMemberAsync(boardId, User.GetUserId())) return Forbid();
        var comment = await _commentService.CreateCommentAsync(boardId, taskId, request, User.GetUserId());
        if (comment is null) return NotFound();
        return Ok(comment);
    }

    [HttpDelete("{commentId}")]
    public async Task<IActionResult> DeleteComment(int boardId, int taskId, int commentId)
    {
        if (!await _boardService.IsMemberAsync(boardId, User.GetUserId())) return Forbid();
        var deleted = await _commentService.DeleteCommentAsync(boardId, taskId, commentId, User.GetUserId());
        if (!deleted) return NotFound();
        return NoContent();
    }
}
