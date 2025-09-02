# Custom Authentication Middleware

This project now uses custom authentication middleware instead of the built-in ASP.NET Core JWT authentication. This provides more control over the authentication process and allows for custom business logic.

## Features

### 1. **Custom Authentication Middleware** (`AuthenticationMiddleware.cs`)
- **JWT Token Validation**: Validates JWT tokens from multiple sources
- **Multiple Token Sources**: Supports Authorization header, query string, and custom headers
- **Database Validation**: Ensures user still exists and is active in database
- **Public Endpoints**: Automatically skips authentication for public routes
- **User Context**: Sets authenticated user information in HttpContext

### 2. **Custom Authorization Middleware** (`AuthorizationMiddleware.cs`)
- **Role-Based Access Control**: Implements role checking logic
- **Route-Based Authorization**: Automatically checks roles based on URL patterns
- **Custom Header Support**: Allows specifying required roles via headers
- **Flexible Role System**: Easy to extend for different role types

### 3. **Current User Service** (`CurrentUserService.cs`)
- **Easy Access**: Provides simple access to current user information
- **Type Safety**: Strongly typed user properties
- **Dependency Injection**: Easily injectable into controllers and services

### 4. **Custom Authorization Attribute** (`AuthorizeRolesAttribute.cs`)
- **Controller-Level**: Apply to entire controllers
- **Action-Level**: Apply to specific actions
- **Multiple Roles**: Support for multiple required roles
- **Custom Logic**: Extensible role checking logic

## Configuration

### Program.cs Changes

```csharp
// Remove built-in JWT authentication
// builder.Services.AddAuthentication(...)

// Add HttpContextAccessor for CurrentUserService
builder.Services.AddHttpContextAccessor();

// Register CurrentUserService
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// In middleware pipeline
app.UseCustomAuthentication();
app.UseCustomAuthorization();
```

### Public Endpoints

The following endpoints are automatically considered public (no authentication required):
- `/api/v1/auth/login`
- `/api/v1/auth/register`
- `/swagger/*`
- Any endpoint you add to the `IsPublicEndpoint` method

## Usage Examples

### 1. **Basic Authentication**

```csharp
[ApiController]
[Route("api/[controller]")]
[AuthorizeRoles("User")] // Requires basic user role
public class ProductsController : ControllerBase
{
    private readonly ICurrentUserService _currentUserService;

    public ProductsController(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public IActionResult GetProducts()
    {
        if (!_currentUserService.IsAuthenticated)
        {
            return Unauthorized();
        }

        var userId = _currentUserService.UserId;
        var userEmail = _currentUserService.UserEmail;
        
        // Your logic here
        return Ok($"Products for user {userEmail}");
    }
}
```

### 2. **Role-Based Access Control**

```csharp
[ApiController]
[Route("api/admin")]
[AuthorizeRoles("Admin")] // Only admin users
public class AdminController : ControllerBase
{
    [HttpGet("dashboard")]
    public IActionResult GetDashboard()
    {
        var currentUser = _currentUserService.User;
        return Ok($"Welcome Admin {currentUser.FirstName}!");
    }
}

[ApiController]
[Route("api/manager")]
[AuthorizeRoles("Admin", "Manager")] // Admin OR Manager
public class ManagerController : ControllerBase
{
    [HttpGet("reports")]
    public IActionResult GetReports()
    {
        return Ok("Manager reports");
    }
}
```

### 3. **Dynamic Role Requirements**

```csharp
// Set required roles via custom header
// X-Required-Roles: Admin,Manager

// Or use route patterns
// /api/admin/* -> Requires Admin role
// /api/manager/* -> Requires Admin or Manager role
```

### 4. **Accessing Current User**

```csharp
public class SomeService
{
    private readonly ICurrentUserService _currentUserService;

    public SomeService(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    public void DoSomething()
    {
        if (_currentUserService.IsAuthenticated)
        {
            var userId = _currentUserService.UserId;
            var userEmail = _currentUserService.UserEmail;
            var user = _currentUserService.User;
            
            // Use user information
        }
    }
}
```

## Token Sources

The middleware automatically extracts JWT tokens from:

1. **Authorization Header**: `Authorization: Bearer <token>`
2. **Query String**: `?token=<jwt_token>`
3. **Custom Header**: `X-Auth-Token: <jwt_token>`

## Error Handling

### Authentication Errors (401 Unauthorized)
- No token provided
- Invalid token format
- Expired token
- Invalid signature
- Invalid issuer/audience

### Authorization Errors (403 Forbidden)
- User not authenticated
- Insufficient permissions
- Role requirements not met

## Extending the System

### 1. **Add New Roles**

```csharp
// In AuthorizationMiddleware.cs or AuthorizeRolesAttribute.cs
private bool HasRequiredRoles(User? user, string[] requiredRoles)
{
    foreach (var role in requiredRoles)
    {
        switch (role.ToLower())
        {
            case "admin":
                return user.Email.Contains("admin");
            case "manager":
                return user.Email.Contains("manager");
            case "moderator":
                return user.Email.Contains("moderator");
            case "user":
                return true; // All authenticated users
        }
    }
    return false;
}
```

### 2. **Add New Public Endpoints**

```csharp
private bool IsPublicEndpoint(PathString path)
{
    var publicEndpoints = new[]
    {
        "/api/v1/auth/login",
        "/api/v1/auth/register",
        "/api/v1/public/*", // New public endpoint
        "/health",           // Health check endpoint
        "/swagger"
    };

    return publicEndpoints.Any(endpoint => path.StartsWithSegments(endpoint));
}
```

### 3. **Custom Token Validation**

```csharp
private async Task<User?> ValidateTokenAndGetUser(HttpContext context, string token)
{
    // Add your custom validation logic here
    // For example: check token blacklist, additional claims, etc.
    
    // Existing validation code...
}
```

## Security Considerations

1. **Token Storage**: Store tokens securely on the client side
2. **HTTPS**: Always use HTTPS in production
3. **Token Expiry**: Set appropriate token expiration times
4. **Role Validation**: Validate roles on both client and server side
5. **Audit Logging**: Log authentication and authorization events
6. **Rate Limiting**: Implement rate limiting for authentication endpoints

## Testing

### Test with Swagger
1. Login via `/api/v1/auth/login`
2. Copy the JWT token from the response
3. Click "Authorize" in Swagger
4. Enter `Bearer <your_token>`
5. Test protected endpoints

### Test with Postman
1. Set Authorization header: `Bearer <your_token>`
2. Or add query parameter: `?token=<your_token>`
3. Or set custom header: `X-Auth-Token: <your_token>`

## Migration from Built-in Authentication

If you're migrating from the built-in JWT authentication:

1. **Remove** `app.UseAuthentication()` and `app.UseAuthorization()`
2. **Add** `app.UseCustomAuthentication()` and `app.UseCustomAuthorization()`
3. **Replace** `[Authorize]` with `[AuthorizeRoles("User")]`
4. **Update** controllers to use `ICurrentUserService` instead of `User` property
5. **Test** all endpoints to ensure proper authentication

## Benefits of Custom Middleware

1. **Full Control**: Complete control over authentication logic
2. **Custom Business Rules**: Implement domain-specific authentication rules
3. **Database Integration**: Direct access to user data during authentication
4. **Flexible Token Sources**: Support multiple ways to pass tokens
5. **Custom Error Handling**: Tailored error messages and responses
6. **Performance**: Optimized for your specific use case
7. **Debugging**: Easier to debug and troubleshoot
8. **Extensibility**: Easy to add new features and requirements
