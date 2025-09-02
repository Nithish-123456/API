using MyWebApi.Interfaces;

namespace MyWebApi.Services;

public class ServiceManager : IServiceManager
{
    private readonly Lazy<IUserService> _userService;
    private readonly Lazy<IProductService> _productService;
    private readonly Lazy<IAuthService> _authService;

    public ServiceManager(
        IRepositoryManager repositoryManager,
        IServiceProvider serviceProvider)
    {
        _userService = new Lazy<IUserService>(() => serviceProvider.GetRequiredService<IUserService>());
        _productService = new Lazy<IProductService>(() => serviceProvider.GetRequiredService<IProductService>());
        _authService = new Lazy<IAuthService>(() => serviceProvider.GetRequiredService<IAuthService>());
    }

    public IUserService UserService => _userService.Value;
    public IProductService ProductService => _productService.Value;
    public IAuthService AuthService => _authService.Value;
}