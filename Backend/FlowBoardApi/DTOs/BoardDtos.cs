namespace FlowBoardApi.DTOs;

public record CreateBoardRequest(string Name);

public record BoardResponse(int Id, string Name, int OwnerId, DateTime CreatedAt, List<BoardMemberResponse> Members);

public record BoardMemberResponse(int UserId, string DisplayName, string Email, string Role);

public record AddMemberRequest(string Email);
