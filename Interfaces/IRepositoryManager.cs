namespace MyWebApi.Interfaces;

public interface IRepositoryManager
{
    IUserRepository Users { get; }
    IProductRepository Products { get; }
    Task SaveAsync();
}