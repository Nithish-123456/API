namespace MyWebApi.Interfaces;

public interface IServiceManager
{
    IUserService UserService { get; }
    IProductService ProductService { get; }
    IAuthService AuthService { get; }
}