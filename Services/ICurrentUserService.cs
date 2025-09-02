using MyWebApi.Models;

namespace MyWebApi.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? UserEmail { get; }
    User? User { get; }
    bool IsAuthenticated { get; }
}
