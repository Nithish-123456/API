using MyWebApi.Data;
using MyWebApi.Interfaces;

namespace MyWebApi.Repositories;

public class RepositoryManager : IRepositoryManager
{
    private readonly ApplicationDbContext _context;
    private readonly Lazy<IUserRepository> _userRepository;
    private readonly Lazy<IProductRepository> _productRepository;

    public RepositoryManager(ApplicationDbContext context)
    {
        _context = context;
        _userRepository = new Lazy<IUserRepository>(() => new UserRepository(context));
        _productRepository = new Lazy<IProductRepository>(() => new ProductRepository(context));
    }

    public IUserRepository Users => _userRepository.Value;
    public IProductRepository Products => _productRepository.Value;

    public async Task SaveAsync()
    {
        await _context.SaveChangesAsync();
    }
}