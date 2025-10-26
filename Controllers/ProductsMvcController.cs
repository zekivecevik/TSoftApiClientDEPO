using Microsoft.AspNetCore.Mvc;
using TSoftApiClient.Services;
using TSoftApiClient.DTOs;

namespace TSoftApiClient.Controllers
{
    /// <summary>
    /// Ürün sayfaları için MVC Controller - Geliştirilmiş Versiyon
    /// </summary>
    public class ProductsMvcController : Controller
    {
        private readonly TSoftApiService _tsoftService;
        private readonly ILogger<ProductsMvcController> _logger;

        public ProductsMvcController(
            TSoftApiService tsoftService,
            ILogger<ProductsMvcController> logger)
        {
            _tsoftService = tsoftService;
            _logger = logger;
        }

        /// <summary>
        /// Ürün listesi sayfası - Geliştirilmiş versiyon
        /// Görsel, kategori ağacı ve detaylı filtrelerle
        /// </summary>
        [Route("/Products")]
        [Route("/ProductsMvc")]
        public async Task<IActionResult> Index(int page = 1, int limit = 50)
        {
            try
            {
                _logger.LogInformation("📦 Loading enhanced products page {Page} with limit {Limit}", page, limit);

                // Get enhanced products with images and category info
                var result = await _tsoftService.GetEnhancedProductsAsync(
                    limit: limit,
                    page: page,
                    includeImages: true
                );

                if (!result.Success)
                {
                    var errorMsg = result.Message?.FirstOrDefault()?.Text?.FirstOrDefault() ?? "Bilinmeyen hata";
                    _logger.LogError("❌ Products API failed: {Error}", errorMsg);
                    ViewBag.Error = $"Ürünler yüklenemedi: {errorMsg}";
                    return View("~/Views/Products/Index.cshtml", new List<Models.Product>());
                }

                var products = result.Data ?? new List<Models.Product>();
                _logger.LogInformation("✅ Loaded {Count} products", products.Count);

                // İstatistikler
                ViewBag.TotalProducts = products.Count;
                ViewBag.ActiveProducts = products.Count(p => p.IsActive == "1");
                ViewBag.TotalStock = products.Sum(p => int.TryParse(p.Stock, out var s) ? s : 0);

                decimal totalValue = 0;
                foreach (var p in products)
                {
                    if (int.TryParse(p.Stock, out var stock) &&
                        decimal.TryParse(p.SellingPrice ?? p.Price,
                            System.Globalization.NumberStyles.Any,
                            System.Globalization.CultureInfo.InvariantCulture, out var price))
                    {
                        totalValue += stock * price;
                    }
                }
                ViewBag.TotalValue = totalValue;

                // Kategorileri al
                var categoriesResult = await _tsoftService.GetCategoryTreeAsync();
                ViewBag.Categories = categoriesResult.Success ? categoriesResult.Data : new List<Models.Category>();

                // Markaları çıkar
                ViewBag.Brands = products
                    .Select(p => p.Brand)
                    .Where(b => !string.IsNullOrEmpty(b))
                    .Distinct()
                    .OrderBy(b => b)
                    .ToList();

                // Pagination info
                ViewBag.CurrentPage = page;
                ViewBag.PageSize = limit;
                ViewBag.HasMore = products.Count >= limit;

                return View("~/Views/Products/Index.cshtml", products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Exception while loading products");
                ViewBag.Error = $"Bir hata oluştu: {ex.Message}";
                return View("~/Views/Products/Index.cshtml", new List<Models.Product>());
            }
        }

        /// <summary>
        /// Ürün ekleme sayfası
        /// </summary>
        [Route("/Products/Create")]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            // Kategorileri yükle
            var categories = await _tsoftService.GetCategoriesAsync();
            ViewBag.Categories = categories.Data ?? new List<Models.Category>();

            return View("~/Views/Products/Create.cshtml");
        }

        /// <summary>
        /// Ürün ekleme işlemi
        /// </summary>
        [Route("/Products/Create")]
        [HttpPost]
        public async Task<IActionResult> Create(CreateProductDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var categories = await _tsoftService.GetCategoriesAsync();
                    ViewBag.Categories = categories.Data ?? new List<Models.Category>();
                    return View("~/Views/Products/Create.cshtml", dto);
                }

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

                if (result.Success)
                {
                    TempData["Success"] = "Ürün başarıyla eklendi!";
                    return RedirectToAction("Index");
                }
                else
                {
                    var message = result.Message?.FirstOrDefault()?.Text?.FirstOrDefault() ?? "Bilinmeyen hata";
                    ViewBag.Error = message;

                    var categories = await _tsoftService.GetCategoriesAsync();
                    ViewBag.Categories = categories.Data ?? new List<Models.Category>();
                    return View("~/Views/Products/Create.cshtml", dto);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ürün eklenirken hata");
                ViewBag.Error = "Bir hata oluştu: " + ex.Message;

                var categories = await _tsoftService.GetCategoriesAsync();
                ViewBag.Categories = categories.Data ?? new List<Models.Category>();
                return View("~/Views/Products/Create.cshtml", dto);
            }
        }
    }
}