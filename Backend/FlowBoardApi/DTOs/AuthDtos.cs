namespace FlowBoardApi.DTOs;

public record RegisterRequest(string Email, string Password, string DisplayName);
public record LoginRequest(string Email, string Password);
public record AuthResponse(int Id, string Token, string Email, string DisplayName);
