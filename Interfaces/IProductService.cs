using MyWebApi.Common;
using MyWebApi.DTOs;

namespace MyWebApi.Interfaces;

public interface IProductService
{
    Task<ApiResponse<ProductDto>> GetProductByIdAsync(Guid id);
    Task<ApiResponse<PagedResult<ProductDto>>> GetProductsAsync(FilterParameters parameters);
    Task<ApiResponse<ProductDto>> CreateProductAsync(CreateProductDto createProductDto);
    Task<ApiResponse<ProductDto>> UpdateProductAsync(Guid id, UpdateProductDto updateProductDto);
    Task<ApiResponse<bool>> DeleteProductAsync(Guid id);
    Task<ApiResponse<IEnumerable<ProductDto>>> GetActiveProductsAsync();
}