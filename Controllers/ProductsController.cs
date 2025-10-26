using Microsoft.AspNetCore.Mvc;
using TSoftApiClient.DTOs;
using TSoftApiClient.Models;
using TSoftApiClient.Services;

namespace TSoftApiClient.Controllers
{
    /// <summary>
    /// Ürün işlemleri için API Controller
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class ProductsController : ControllerBase
    {
        private readonly TSoftApiService _tsoftService;
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(
            TSoftApiService tsoftService,
            ILogger<ProductsController> logger)
        {
            _tsoftService = tsoftService;
            _logger = logger;
        }

        /// <summary>
        /// Tüm ürünleri listeler
        /// </summary>
        /// <param name="limit">Maksimum ürün sayısı (varsayılan: 50)</param>
        /// <returns>Ürün listesi</returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<TSoftApiResponse<List<Product>>>> GetProducts(
            [FromQuery] int limit = 50)
        {
            _logger.LogInformation("Getting products with limit: {Limit}", limit);
            
            var result = await _tsoftService.GetProductsAsync(limit);
            
            if (!result.Success)
            {
                _logger.LogWarning("Failed to get products");
                return StatusCode(500, result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Tek ürün ekler
        /// </summary>
        /// <param name="dto">Ürün bilgileri</param>
        /// <returns>Ekleme sonucu</returns>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<TSoftApiResponse<Product>>> CreateProduct(
            [FromBody] CreateProductDto dto)
        {
            _logger.LogInformation("Creating product: {Code} - {Name}", dto.Code, dto.Name);

            var extraFields = new Dictionary<string, string>();
            
            if (!string.IsNullOrEmpty(dto.Brand))
                extraFields["Brand"] = dto.Brand;
            if (!string.IsNullOrEmpty(dto.Vat))
                extraFields["Vat"] = dto.Vat;
            if (!string.IsNullOrEmpty(dto.Currency))
                extraFields["Currency"] = dto.Currency;
            if (!string.IsNullOrEmpty(dto.BuyingPrice))
                extraFields["BuyingPrice"] = dto.BuyingPrice;
            if (!string.IsNullOrEmpty(dto.ShortDescription))
                extraFields["ShortDescription"] = dto.ShortDescription;

            var result = await _tsoftService.AddProductAsync(
                dto.Code,
                dto.Name,
                dto.CategoryCode,
                dto.Price,
                dto.Stock,
                extraFields
            );

            if (!result.Success)
            {
                _logger.LogWarning("Failed to create product: {Code}", dto.Code);
                return StatusCode(500, result);
            }

            _logger.LogInformation("Product created successfully: {Code}", dto.Code);
            return CreatedAtAction(nameof(GetProducts), new { limit = 1 }, result);
        }

        /// <summary>
        /// Toplu ürün ekler
        /// </summary>
        /// <param name="dto">Ürün listesi</param>
        /// <returns>Ekleme sonucu</returns>
        [HttpPost("bulk")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<TSoftApiResponse<object>>> CreateBulkProducts(
            [FromBody] BulkCreateProductDto dto)
        {
            _logger.LogInformation("Creating {Count} products in bulk", dto.Products.Count);

            // DTO'dan Product modeline dönüştür
            var products = dto.Products.Select(p => new Product
            {
                ProductCode = p.Code,
                ProductName = p.Name,
                DefaultCategoryCode = p.CategoryCode,
                Stock = p.Stock.ToString(),
                SellingPrice = p.Price.ToString("F2"),
                IsActive = "1",
                StockUnit = "adet",
                Brand = p.Brand,
                Vat = p.Vat,
                Currency = p.Currency,
                BuyingPrice = p.BuyingPrice,
                ShortDescription = p.ShortDescription
            }).ToList();

            var result = await _tsoftService.CreateProductsAsync(products);

            if (!result.Success)
            {
                _logger.LogWarning("Failed to create bulk products");
                return StatusCode(500, result);
            }

            _logger.LogInformation("Bulk products created successfully");
            return CreatedAtAction(nameof(GetProducts), new { limit = dto.Products.Count }, result);
        }
    }
}
