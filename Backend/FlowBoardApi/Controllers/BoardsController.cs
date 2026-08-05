using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FlowBoardApi.DTOs;
using FlowBoardApi.Extensions;
using FlowBoardApi.Services;

namespace FlowBoardApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BoardsController : ControllerBase
{
    private readonly IBoardService _boardService;

    public BoardsController(IBoardService boardService)
    {
        _boardService = boardService;
    }

    [HttpGet]
    public async Task<ActionResult<List<BoardResponse>>> GetMyBoards()
    {
        var boards = await _boardService.GetBoardsForUserAsync(User.GetUserId());
        return Ok(boards);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<BoardResponse>> GetBoard(int id)
    {
        var board = await _boardService.GetBoardAsync(id, User.GetUserId());
        if (board is null) return NotFound();
        return Ok(board);
    }

    [HttpPost]
    public async Task<ActionResult<BoardResponse>> CreateBoard(CreateBoardRequest request)
    {
        var board = await _boardService.CreateBoardAsync(request, User.GetUserId());
        return CreatedAtAction(nameof(GetBoard), new { id = board.Id }, board);
    }

    [HttpPost("{id}/members")]
    public async Task<ActionResult<BoardResponse>> AddMember(int id, AddMemberRequest request)
    {
        var board = await _boardService.AddMemberAsync(id, request, User.GetUserId());
        if (board is null) return Forbid();
        return Ok(board);
    }

    [HttpDelete("{id}/members/{userId}")]
    public async Task<ActionResult<BoardResponse>> RemoveMember(int id, int userId)
    {
        var board = await _boardService.RemoveMemberAsync(id, userId, User.GetUserId());
        if (board is null) return Forbid();
        return Ok(board);
    }

    [HttpPost("{id}/leave")]
    public async Task<ActionResult<BoardResponse>> LeaveBoard(int id)
    {
        var board = await _boardService.LeaveBoardAsync(id, User.GetUserId());
        if (board is null) return Forbid();
        return Ok(board);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteBoard(int id)
    {
        var deleted = await _boardService.DeleteBoardAsync(id, User.GetUserId());
        if (!deleted) return Forbid();
        return NoContent();
    }
}
