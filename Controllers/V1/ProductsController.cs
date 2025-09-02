using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyWebApi.Common;
using MyWebApi.DTOs;
using MyWebApi.Interfaces;

namespace MyWebApi.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class ProductsController : ControllerBase
{
    private readonly IServiceManager _serviceManager;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(IServiceManager serviceManager, ILogger<ProductsController> logger)
    {
        _serviceManager = serviceManager;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<ProductDto>>>> GetProducts([FromQuery] FilterParameters parameters)
    {
        var result = await _serviceManager.ProductService.GetProductsAsync(parameters);
        return Ok(result);
    }

    [HttpGet("active")]
    public async Task<ActionResult<ApiResponse<IEnumerable<ProductDto>>>> GetActiveProducts()
    {
        var result = await _serviceManager.ProductService.GetActiveProductsAsync();
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<ProductDto>>> GetProduct(Guid id)
    {
        var result = await _serviceManager.ProductService.GetProductByIdAsync(id);
        
        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ProductDto>>> CreateProduct([FromBody] CreateProductDto createProductDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<ProductDto>.ErrorResponse("Invalid model state"));
        }

        var result = await _serviceManager.ProductService.CreateProductAsync(createProductDto);
        
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return CreatedAtAction(nameof(GetProduct), new { id = result.Data!.Id }, result);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<ProductDto>>> UpdateProduct(Guid id, [FromBody] UpdateProductDto updateProductDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<ProductDto>.ErrorResponse("Invalid model state"));
        }

        var result = await _serviceManager.ProductService.UpdateProductAsync(id, updateProductDto);
        
        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteProduct(Guid id)
    {
        var result = await _serviceManager.ProductService.DeleteProductAsync(id);
        
        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }
}