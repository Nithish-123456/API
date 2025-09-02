using Microsoft.AspNetCore.Http;
using MyWebApi.Interfaces;
using MyWebApi.Models;

namespace MyWebApi.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? UserId
    {
        get
        {
            var userId = _httpContextAccessor.HttpContext?.Items["UserId"];
            return userId as Guid?;
        }
    }

    public string? UserEmail
    {
        get
        {
            var userEmail = _httpContextAccessor.HttpContext?.Items["UserEmail"];
            return userEmail as string;
        }
    }

    public User? User
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.Items["User"];
            return user as User;
        }
    }

    public bool IsAuthenticated
    {
        get
        {
            return UserId.HasValue && User != null;
        }
    }
}
