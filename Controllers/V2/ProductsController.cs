using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyWebApi.Common;
using MyWebApi.DTOs;
using MyWebApi.Interfaces;

namespace MyWebApi.Controllers.V2;

[ApiController]
[ApiVersion("2.0")]
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
        // V2 might have enhanced filtering or different response format
        var result = await _serviceManager.ProductService.GetProductsAsync(parameters);
        
        // Add version info to response
        if (result.Success && result.Data != null)
        {
            Response.Headers.Add("API-Version", "2.0");
            Response.Headers.Add("X-Total-Count", result.Data.TotalCount.ToString());
        }
        
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

        Response.Headers.Add("API-Version", "2.0");
        return Ok(result);
    }
}