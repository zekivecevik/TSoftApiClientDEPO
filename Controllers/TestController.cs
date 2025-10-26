using Microsoft.AspNetCore.Mvc;
using TSoftApiClient.Services;

namespace TSoftApiClient.Controllers
{
    /// <summary>
    /// API Test sayfası
    /// </summary>
    public class TestController : Controller
    {
        private readonly TSoftApiService _tsoftService;
        private readonly ILogger<TestController> _logger;

        public TestController(
            TSoftApiService tsoftService,
            ILogger<TestController> logger)
        {
            _tsoftService = tsoftService;
            _logger = logger;
        }

        /// <summary>
        /// API Test sayfası
        /// </summary>
        [Route("/Test")]
        public async Task<IActionResult> Index()
        {
            var logs = new List<string>();

            try
            {
                logs.Add("🔍 T-Soft API V3 Test Başladı");
                logs.Add("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                logs.Add("");

                // Test 1: Ürünleri çek
                logs.Add("📦 Test 1: Ürün Listesi");
                logs.Add("Endpoint: GET /product/getProducts (REST1)");
                logs.Add("");

                var productsResult = await _tsoftService.GetProductsAsync(limit: 5);
                
                logs.Add($"✅ Success: {productsResult.Success}");
                logs.Add($"📊 Data Count: {productsResult.Data?.Count ?? 0}");
                
                if (productsResult.Data != null && productsResult.Data.Count > 0)
                {
                    logs.Add("");
                    logs.Add("İlk ürün:");
                    var firstProduct = productsResult.Data.First();
                    logs.Add($"  - ProductCode: {firstProduct.ProductCode}");
                    logs.Add($"  - ProductName: {firstProduct.ProductName}");
                    logs.Add($"  - Price: {firstProduct.Price}");
                    logs.Add($"  - SellingPrice: {firstProduct.SellingPrice}");
                    logs.Add($"  - Stock: {firstProduct.Stock}");
                }
                else if (productsResult.Message != null)
                {
                    logs.Add("❌ Hata Mesajı:");
                    foreach (var msg in productsResult.Message)
                    {
                        if (msg.Text != null)
                        {
                            foreach (var text in msg.Text)
                            {
                                logs.Add($"  {text}");
                            }
                        }
                    }
                }

                logs.Add("");
                logs.Add("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                logs.Add("");

                // Test 2: Kategorileri çek
                logs.Add("📁 Test 2: Kategori Listesi");
                logs.Add("Endpoint: GET /category/getCategories (REST1)");
                logs.Add("");

                var categoriesResult = await _tsoftService.GetCategoriesAsync();
                
                logs.Add($"✅ Success: {categoriesResult.Success}");
                logs.Add($"📊 Data Count: {categoriesResult.Data?.Count ?? 0}");
                
                if (categoriesResult.Data != null && categoriesResult.Data.Count > 0)
                {
                    logs.Add("");
                    logs.Add("İlk 3 kategori:");
                    foreach (var cat in categoriesResult.Data.Take(3))
                    {
                        logs.Add($"  - [{cat.CategoryCode}] {cat.CategoryName}");
                    }
                }

                logs.Add("");
                logs.Add("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                logs.Add("");

                // Test 3: Siparişleri çek
                logs.Add("🛒 Test 3: Sipariş Listesi");
                logs.Add("Endpoint: GET /order/getOrders (REST1)");
                logs.Add("");

                var ordersResult = await _tsoftService.GetOrdersAsync(limit: 5);
                
                logs.Add($"✅ Success: {ordersResult.Success}");
                logs.Add($"📊 Data Count: {ordersResult.Data?.Count ?? 0}");
                
                if (ordersResult.Data != null && ordersResult.Data.Count > 0)
                {
                    logs.Add("");
                    logs.Add("İlk sipariş:");
                    var firstOrder = ordersResult.Data.First();
                    logs.Add($"  - OrderCode: {firstOrder.OrderCode}");
                    logs.Add($"  - OrderId: {firstOrder.OrderId}");
                    logs.Add($"  - CustomerName: {firstOrder.CustomerName}");
                    logs.Add($"  - OrderDate: {firstOrder.OrderDate}");
                    logs.Add($"  - Total: {firstOrder.Total}");
                    logs.Add($"  - Status: {firstOrder.Status}");
                    logs.Add($"  - ItemCount: {firstOrder.ItemCount}");
                }
                else if (ordersResult.Message != null)
                {
                    logs.Add("❌ Hata Mesajı:");
                    foreach (var msg in ordersResult.Message)
                    {
                        if (msg.Text != null)
                        {
                            foreach (var text in msg.Text)
                            {
                                logs.Add($"  {text}");
                            }
                        }
                    }
                }

                ViewBag.Logs = logs;
                ViewBag.ProductsResult = productsResult;
                ViewBag.CategoriesResult = categoriesResult;
                ViewBag.OrdersResult = ordersResult;
            }
            catch (Exception ex)
            {
                logs.Add($"💥 HATA: {ex.Message}");
                logs.Add($"Stack: {ex.StackTrace}");
                ViewBag.Logs = logs;
            }

            return View("~/Views/Test/Index.cshtml");
        }
    }
}
