using Microsoft.AspNetCore.Mvc;
using TSoftApiClient.Services;
using TSoftApiClient.DTOs;

namespace TSoftApiClient.Controllers
{
    /// <summary>
    /// Ürün sayfaları için MVC Controller - Basitleştirilmiş Versiyon (Görselsiz)
    /// ✅ VARYANT DESTEĞİ EKLENDİ
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
        /// Ürün listesi sayfası - Basitleştirilmiş versiyon (Görselsiz)
        /// </summary>
        [Route("/Products")]
        [Route("/ProductsMvc")]
        public async Task<IActionResult> Index(int page = 1, int limit = 100)
        {
            try
            {
                _logger.LogInformation("📦 Loading products page {Page} with limit {Limit}", page, limit);

                // ✅ includeImages: false - Görsel çekme, hata olmasın
                var result = await _tsoftService.GetEnhancedProductsAsync(
                    limit: limit,
                    page: page,
                    includeImages: false  // ✅ GÖRSEL KAPALI
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
                _logger.LogError(ex, "💥 Exception while loading products. Details: {Message} | StackTrace: {StackTrace}",
                    ex.Message, ex.StackTrace);
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

        // ========== 🎨 VARYANT YÖNETİMİ ==========

        /// <summary>
        /// Ürün detay sayfası - Varyantlarla (Renk-Beden)
        /// </summary>
        [Route("/Products/Detail/{productCode}")]
        [HttpGet]
        public async Task<IActionResult> Detail(string productCode)
        {
            try
            {
                _logger.LogInformation("🎨 Loading product detail with variants: {Code}", productCode);

                var result = await _tsoftService.GetProductWithVariantsAsync(productCode);

                if (!result.Success || result.Data == null)
                {
                    var errorMsg = result.Message?.FirstOrDefault()?.Text?.FirstOrDefault() ?? "Ürün bulunamadı";
                    _logger.LogError("❌ Product detail failed: {Error}", errorMsg);
                    ViewBag.Error = errorMsg;
                    return View("~/Views/Products/Detail.cshtml", new Models.Product());
                }

                var product = result.Data;
                _logger.LogInformation("✅ Loaded product with {Count} variants", product.Variants.Count);

                return View("~/Views/Products/Detail.cshtml", product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Exception while loading product detail: {Code}", productCode);
                ViewBag.Error = $"Bir hata oluştu: {ex.Message}";
                return View("~/Views/Products/Detail.cshtml", new Models.Product());
            }
        }

        /// <summary>
        /// Varyantlı ürünleri listeler (TEST SAYFASI)
        /// </summary>
        [Route("/Products/WithVariants")]
        [HttpGet]
        public async Task<IActionResult> WithVariants()
        {
            var result = await _tsoftService.GetProductsAsync(limit: 100);

            if (!result.Success)
            {
                return Content("<h1>API Hatası</h1><p>Ürünler yüklenemedi.</p>", "text/html");
            }

            var variantProducts = result.Data?
                .Where(p => p.HasSubProducts == "true" || p.HasSubProducts == "1")
                .ToList() ?? new List<Models.Product>();

            var html = @"
<!DOCTYPE html>
<html lang='tr'>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Varyantlı Ürünler</title>
    <style>
        * { margin: 0; padding: 0; box-sizing: border-box; }
        body { 
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            min-height: 100vh;
            padding: 20px;
        }
        .container {
            max-width: 1400px;
            margin: 0 auto;
            background: white;
            border-radius: 16px;
            padding: 40px;
            box-shadow: 0 20px 60px rgba(0,0,0,0.3);
        }
        h1 {
            font-size: 32px;
            color: #2c3e50;
            margin-bottom: 10px;
            display: flex;
            align-items: center;
            gap: 12px;
        }
        .subtitle {
            color: #6c757d;
            font-size: 16px;
            margin-bottom: 30px;
        }
        .back-btn {
            display: inline-block;
            padding: 12px 24px;
            background: #667eea;
            color: white;
            text-decoration: none;
            border-radius: 8px;
            font-weight: 600;
            margin-bottom: 20px;
            transition: all 0.3s;
        }
        .back-btn:hover {
            background: #5568d3;
            transform: translateY(-2px);
            box-shadow: 0 4px 12px rgba(102, 126, 234, 0.4);
        }
        table { 
            border-collapse: separate;
            border-spacing: 0;
            width: 100%;
            border-radius: 12px;
            overflow: hidden;
            box-shadow: 0 2px 8px rgba(0,0,0,0.1);
        }
        th, td { 
            padding: 16px;
            text-align: left;
        }
        th { 
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            font-weight: 600;
            font-size: 14px;
            text-transform: uppercase;
            letter-spacing: 0.5px;
        }
        tbody tr {
            border-bottom: 1px solid #f0f0f0;
            transition: all 0.2s;
        }
        tbody tr:hover {
            background: #f8f9fa;
        }
        td {
            font-size: 14px;
            color: #2c3e50;
        }
        a { 
            color: #667eea;
            text-decoration: none;
            font-weight: 600;
            transition: all 0.2s;
        }
        a:hover { 
            color: #5568d3;
            text-decoration: underline;
        }
        .badge {
            display: inline-block;
            padding: 4px 12px;
            background: #d4edda;
            color: #155724;
            border-radius: 12px;
            font-size: 12px;
            font-weight: 600;
        }
        .empty-state {
            text-align: center;
            padding: 60px 20px;
            background: #f8f9fa;
            border-radius: 12px;
            border: 2px dashed #dee2e6;
        }
        .empty-state-icon {
            font-size: 64px;
            margin-bottom: 20px;
        }
    </style>
</head>
<body>
    <div class='container'>
        <a href='/Products' class='back-btn'>← Ürünlere Dön</a>
        <h1>🎨 Varyantlı Ürünler</h1>
        <p class='subtitle'><strong>Toplam:</strong> " + variantProducts.Count + @" ürün bulundu</p>";

            if (variantProducts.Count > 0)
            {
                html += @"
        <table>
            <thead>
                <tr>
                    <th>Ürün Kodu</th>
                    <th>Ürün Adı</th>
                    <th>Alt Ürün Durumu</th>
                    <th>İşlem</th>
                </tr>
            </thead>
            <tbody>";

                foreach (var p in variantProducts)
                {
                    html += $@"
                <tr>
                    <td><strong>{p.ProductCode}</strong></td>
                    <td>{p.ProductName}</td>
                    <td><span class='badge'>✓ Varyantlı</span></td>
                    <td><a href='/Products/Detail/{p.ProductCode}'>Detay Göster →</a></td>
                </tr>";
                }

                html += @"
            </tbody>
        </table>";
            }
            else
            {
                html += @"
        <div class='empty-state'>
            <div class='empty-state-icon'>📦</div>
            <h3>Varyantlı ürün bulunamadı</h3>
            <p>Sistemde renk veya beden seçeneği olan ürün yok.</p>
        </div>";
            }

            html += @"
    </div>
</body>
</html>";

            return Content(html, "text/html");
        }
    }
}