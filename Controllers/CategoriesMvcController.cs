using Microsoft.AspNetCore.Mvc;
using TSoftApiClient.Services;

namespace TSoftApiClient.Controllers
{
    /// <summary>
    /// Kategori sayfaları için MVC Controller
    /// </summary>
    public class CategoriesMvcController : Controller
    {
        private readonly TSoftApiService _tsoftService;
        private readonly ILogger<CategoriesMvcController> _logger;

        public CategoriesMvcController(
            TSoftApiService tsoftService,
            ILogger<CategoriesMvcController> logger)
        {
            _tsoftService = tsoftService;
            _logger = logger;
        }

        /// <summary>
        /// Kategori listesi sayfası
        /// </summary>
        [Route("/Categories")]
        [Route("/CategoriesMvc")]
        public async Task<IActionResult> Index()
        {
            try
            {
                var result = await _tsoftService.GetCategoriesAsync();
                
                if (!result.Success)
                {
                    ViewBag.Error = "Kategoriler yüklenemedi";
                    return View("~/Views/Categories/Index.cshtml", new List<Models.Category>());
                }

                return View("~/Views/Categories/Index.cshtml", result.Data ?? new List<Models.Category>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kategoriler yüklenirken hata");
                ViewBag.Error = "Bir hata oluştu";
                return View("~/Views/Categories/Index.cshtml", new List<Models.Category>());
            }
        }
    }
}
