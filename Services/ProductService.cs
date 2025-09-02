using AutoMapper;
using Microsoft.Extensions.Caching.Memory;
using MyWebApi.Common;
using MyWebApi.DTOs;
using MyWebApi.Interfaces;
using MyWebApi.Models;
using System.Linq.Expressions;

namespace MyWebApi.Services;

public class ProductService : IProductService
{
    private readonly IRepositoryManager _repositoryManager;
    private readonly IMapper _mapper;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ProductService> _logger;
    private const string ProductCacheKeyPrefix = "product_";
    private const string ActiveProductsCacheKey = "active_products";
    private const int CacheExpirationMinutes = 15;

    public ProductService(
        IRepositoryManager repositoryManager,
        IMapper mapper,
        IMemoryCache cache,
        ILogger<ProductService> logger)
    {
        _repositoryManager = repositoryManager;
        _mapper = mapper;
        _cache = cache;
        _logger = logger;
    }

    public async Task<ApiResponse<ProductDto>> GetProductByIdAsync(Guid id)
    {
        try
        {
            var cacheKey = $"{ProductCacheKeyPrefix}{id}";
            
            if (_cache.TryGetValue(cacheKey, out ProductDto? cachedProduct) && cachedProduct != null)
            {
                _logger.LogInformation("Product {ProductId} retrieved from cache", id);
                return ApiResponse<ProductDto>.SuccessResponse(cachedProduct);
            }

            var product = await _repositoryManager.Products.GetByIdAsync(id);
            if (product == null)
            {
                return ApiResponse<ProductDto>.ErrorResponse("Product not found");
            }

            var productDto = _mapper.Map<ProductDto>(product);
            
            _cache.Set(cacheKey, productDto, TimeSpan.FromMinutes(CacheExpirationMinutes));
            _logger.LogInformation("Product {ProductId} cached for {Minutes} minutes", id, CacheExpirationMinutes);

            return ApiResponse<ProductDto>.SuccessResponse(productDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving product {ProductId}", id);
            return ApiResponse<ProductDto>.ErrorResponse("An error occurred while retrieving the product");
        }
    }

    public async Task<ApiResponse<PagedResult<ProductDto>>> GetProductsAsync(FilterParameters parameters)
    {
        try
        {
            Expression<Func<Product, bool>>? filter = null;

            if (!string.IsNullOrEmpty(parameters.SearchTerm))
            {
                var searchTerm = parameters.SearchTerm.ToLower();
                filter = p => p.Name.ToLower().Contains(searchTerm) ||
                             p.Description.ToLower().Contains(searchTerm);
            }

            var pagedProducts = await _repositoryManager.Products.GetFilteredAsync(parameters, filter);
            var productDtos = _mapper.Map<IEnumerable<ProductDto>>(pagedProducts.Data);

            var result = new PagedResult<ProductDto>
            {
                Data = productDtos,
                TotalCount = pagedProducts.TotalCount,
                PageNumber = pagedProducts.PageNumber,
                PageSize = pagedProducts.PageSize
            };

            return ApiResponse<PagedResult<ProductDto>>.SuccessResponse(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving products");
            return ApiResponse<PagedResult<ProductDto>>.ErrorResponse("An error occurred while retrieving products");
        }
    }

    public async Task<ApiResponse<ProductDto>> CreateProductAsync(CreateProductDto createProductDto)
    {
        try
        {
            var product = _mapper.Map<Product>(createProductDto);

            await _repositoryManager.Products.AddAsync(product);
            await _repositoryManager.SaveAsync();

            // Invalidate active products cache
            _cache.Remove(ActiveProductsCacheKey);

            var productDto = _mapper.Map<ProductDto>(product);
            _logger.LogInformation("Product {ProductId} created successfully", product.Id);

            return ApiResponse<ProductDto>.SuccessResponse(productDto, "Product created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product");
            return ApiResponse<ProductDto>.ErrorResponse("An error occurred while creating the product");
        }
    }

    public async Task<ApiResponse<ProductDto>> UpdateProductAsync(Guid id, UpdateProductDto updateProductDto)
    {
        try
        {
            var product = await _repositoryManager.Products.GetByIdAsync(id);
            if (product == null)
            {
                return ApiResponse<ProductDto>.ErrorResponse("Product not found");
            }

            _mapper.Map(updateProductDto, product);
            await _repositoryManager.Products.UpdateAsync(product);
            await _repositoryManager.SaveAsync();

            // Invalidate caches
            var cacheKey = $"{ProductCacheKeyPrefix}{id}";
            _cache.Remove(cacheKey);
            _cache.Remove(ActiveProductsCacheKey);

            var productDto = _mapper.Map<ProductDto>(product);
            _logger.LogInformation("Product {ProductId} updated successfully", id);

            return ApiResponse<ProductDto>.SuccessResponse(productDto, "Product updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product {ProductId}", id);
            return ApiResponse<ProductDto>.ErrorResponse("An error occurred while updating the product");
        }
    }

    public async Task<ApiResponse<bool>> DeleteProductAsync(Guid id)
    {
        try
        {
            if (!await _repositoryManager.Products.ExistsAsync(id))
            {
                return ApiResponse<bool>.ErrorResponse("Product not found");
            }

            await _repositoryManager.Products.DeleteAsync(id);
            await _repositoryManager.SaveAsync();

            // Invalidate caches
            var cacheKey = $"{ProductCacheKeyPrefix}{id}";
            _cache.Remove(cacheKey);
            _cache.Remove(ActiveProductsCacheKey);

            _logger.LogInformation("Product {ProductId} deleted successfully", id);
            return ApiResponse<bool>.SuccessResponse(true, "Product deleted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting product {ProductId}", id);
            return ApiResponse<bool>.ErrorResponse("An error occurred while deleting the product");
        }
    }

    public async Task<ApiResponse<IEnumerable<ProductDto>>> GetActiveProductsAsync()
    {
        try
        {
            if (_cache.TryGetValue(ActiveProductsCacheKey, out IEnumerable<ProductDto>? cachedProducts) && cachedProducts != null)
            {
                _logger.LogInformation("Active products retrieved from cache");
                return ApiResponse<IEnumerable<ProductDto>>.SuccessResponse(cachedProducts);
            }

            var products = await _repositoryManager.Products.GetActiveProductsAsync();
            var productDtos = _mapper.Map<IEnumerable<ProductDto>>(products);

            _cache.Set(ActiveProductsCacheKey, productDtos, TimeSpan.FromMinutes(CacheExpirationMinutes));
            _logger.LogInformation("Active products cached for {Minutes} minutes", CacheExpirationMinutes);

            return ApiResponse<IEnumerable<ProductDto>>.SuccessResponse(productDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active products");
            return ApiResponse<IEnumerable<ProductDto>>.ErrorResponse("An error occurred while retrieving active products");
        }
    }
}