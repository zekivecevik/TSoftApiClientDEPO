using Microsoft.AspNetCore.Mvc;
using TSoftApiClient.Models;
using TSoftApiClient.Services;

namespace TSoftApiClient.Controllers
{
    /// <summary>
    /// Kategori işlemleri için API Controller
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class CategoriesController : ControllerBase
    {
        private readonly TSoftApiService _tsoftService;
        private readonly ILogger<CategoriesController> _logger;

        public CategoriesController(
            TSoftApiService tsoftService,
            ILogger<CategoriesController> logger)
        {
            _tsoftService = tsoftService;
            _logger = logger;
        }

        /// <summary>
        /// Tüm kategorileri listeler
        /// </summary>
        /// <returns>Kategori listesi</returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<TSoftApiResponse<List<Category>>>> GetCategories()
        {
            _logger.LogInformation("Getting categories");
            
            var result = await _tsoftService.GetCategoriesAsync();
            
            if (!result.Success)
            {
                _logger.LogWarning("Failed to get categories");
                return StatusCode(500, result);
            }

            _logger.LogInformation("Retrieved {Count} categories", result.Data?.Count ?? 0);
            return Ok(result);
        }
    }
}
