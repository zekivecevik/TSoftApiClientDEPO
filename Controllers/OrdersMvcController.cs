using Microsoft.AspNetCore.Mvc;
using TSoftApiClient.Services;

namespace TSoftApiClient.Controllers
{
    /// <summary>
    /// Sipariş sayfaları için MVC Controller - PAGINATION FIXED
    /// </summary>
    public class OrdersMvcController : Controller
    {
        private readonly TSoftApiService _tsoftService;
        private readonly ILogger<OrdersMvcController> _logger;
        private static bool _detailsApiWorking = true;

        public OrdersMvcController(
            TSoftApiService tsoftService,
            ILogger<OrdersMvcController> logger)
        {
            _tsoftService = tsoftService;
            _logger = logger;
        }

        /// <summary>
        /// Sipariş listesi sayfası - Pagination FIXED
        /// </summary>
        [Route("/Orders")]
        [Route("/OrdersMvc")]
        public async Task<IActionResult> Index(int page = 1, int limit = 100)
        {
            try
            {
                _logger.LogInformation("📦 Fetching orders - Page: {Page}, Limit: {Limit}", page, limit);

                var filters = new Dictionary<string, string>
                {
                    ["page"] = page.ToString(),
                    ["offset"] = ((page - 1) * limit).ToString(),
                    ["start"] = ((page - 1) * limit).ToString()
                };

                var result = await _tsoftService.GetOrdersAsync(limit: limit, filters: filters);

                _logger.LogInformation("📊 Orders API result: Success={Success}, DataCount={Count}",
                    result.Success,
                    result.Data?.Count ?? 0);

                if (!result.Success)
                {
                    var errorMsg = result.Message?.FirstOrDefault()?.Text?.FirstOrDefault() ?? "Bilinmeyen hata";
                    _logger.LogError("❌ Orders API failed: {Error}", errorMsg);
                    ViewBag.Error = $"Siparişler yüklenemedi: {errorMsg}";
                    return View("~/Views/Orders/Index.cshtml", new List<Models.Order>());
                }

                var orders = result.Data ?? new List<Models.Order>();
                _logger.LogInformation("✅ Orders loaded successfully: {Count} orders", orders.Count);

                // DEBUG: İlk siparişin bilgilerini kontrol et
                if (orders.Count > 0)
                {
                    var firstOrder = orders[0];
                    _logger.LogWarning("🏙️ First Order Info: City={City}, ShippingCity={ShippingCity}, OrderStatusId={StatusId}, OrderStatus={Status}, PaymentType={Payment}",
                        firstOrder.City ?? "NULL",
                        firstOrder.ShippingCity ?? "NULL",
                        firstOrder.OrderStatusId ?? "NULL",
                        firstOrder.OrderStatus ?? "NULL",
                        firstOrder.PaymentType ?? "NULL");
                }

                if (_detailsApiWorking && orders.Count > 0)
                {
                    _logger.LogInformation("🔍 Attempting to fetch order details...");

                    var testOrder = orders.First();
                    if (int.TryParse(testOrder.OrderId, out var testOrderId))
                    {
                        var testResult = await _tsoftService.GetOrderDetailsByOrderIdAsync(testOrderId);

                        if (!testResult.Success)
                        {
                            _detailsApiWorking = false;
                            _logger.LogWarning("⚠️ Order details API not available. Disabling future attempts.");
                            ViewBag.Warning = "Sipariş detayları API'sine erişim yok. Ürün sayısı ve paketleme durumu görüntülenemiyor.";
                        }
                        else if (testResult.Data != null && testResult.Data.Count > 0)
                        {
                            _logger.LogInformation("✅ Order details API works! Fetching for all orders...");

                            var successCount = 0;
                            var failCount = 0;

                            var semaphore = new System.Threading.SemaphoreSlim(5);

                            var detailTasks = orders.Select(async order =>
                            {
                                await semaphore.WaitAsync();
                                try
                                {
                                    if (int.TryParse(order.OrderId, out var orderId))
                                    {
                                        var detailsResult = await _tsoftService.GetOrderDetailsByOrderIdAsync(orderId);

                                        if (detailsResult.Success && detailsResult.Data != null)
                                        {
                                            order.OrderDetails = detailsResult.Data;
                                            order.ItemCount = detailsResult.Data.Count;

                                            // Şehir bilgisini DeliveryCity'den al
                                            if (string.IsNullOrEmpty(order.City) && string.IsNullOrEmpty(order.ShippingCity))
                                            {
                                                var firstDetail = detailsResult.Data.FirstOrDefault();
                                                if (firstDetail != null)
                                                {
                                                    order.City = firstDetail.DeliveryCity ?? firstDetail.InvoiceCity ?? firstDetail.City;
                                                    order.ShippingCity = firstDetail.DeliveryCity;
                                                }
                                            }

                                            // Paketleme durumunu doldur
                                            if (string.IsNullOrEmpty(order.SupplyStatus))
                                            {
                                                var firstDetail = detailsResult.Data.FirstOrDefault();
                                                order.SupplyStatus = firstDetail?.SupplyStatus;
                                            }

                                            successCount++;
                                        }
                                        else
                                        {
                                            failCount++;
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogDebug(ex, "Detail fetch failed for order {OrderId}", order.OrderId);
                                    failCount++;
                                }
                                finally
                                {
                                    semaphore.Release();
                                }
                            });

                            await Task.WhenAll(detailTasks);

                            _logger.LogInformation("✅ Details fetched: {Success} success, {Fail} failed",
                                successCount, failCount);

                            if (successCount == 0 && failCount > 0)
                            {
                                ViewBag.Warning = "Sipariş detayları yüklenemedi. API yetki sorunu olabilir.";
                            }
                        }
                        else
                        {
                            _logger.LogInformation("ℹ️ First order has no details (might be empty order)");
                        }
                    }
                }
                else if (!_detailsApiWorking)
                {
                    _logger.LogInformation("ℹ️ Order details API is disabled (previous check failed)");
                    ViewBag.Warning = "Sipariş detayları gösterilemiyor (API yetki sorunu).";
                }

                ViewBag.CurrentPage = page;
                ViewBag.PageSize = limit;
                ViewBag.HasMore = orders.Count >= limit;

                return View("~/Views/Orders/Index.cshtml", orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Exception while loading orders: {Message}", ex.Message);
                ViewBag.Error = $"Bir hata oluştu: {ex.Message}";
                return View("~/Views/Orders/Index.cshtml", new List<Models.Order>());
            }
        }

        [Route("/Orders/ResetApiFlag")]
        public IActionResult ResetApiFlag()
        {
            _detailsApiWorking = true;
            TempData["Success"] = "API flag reset. Details fetching will be attempted again.";
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Sipariş detaylarını çeker (Modal için API endpoint)
        /// GET /api/orders/{orderId}/details
        /// </summary>
        [HttpGet]
        [Route("/api/orders/{orderId}/details")]
        public async Task<IActionResult> GetOrderDetails(int orderId)
        {
            try
            {
                _logger.LogInformation("📦 API: Fetching order details for OrderId: {OrderId}", orderId);

                var result = await _tsoftService.GetOrderDetailsByOrderIdAsync(orderId);

                if (!result.Success || result.Data == null)
                {
                    return Ok(new
                    {
                        success = false,
                        message = "Sipariş detayları bulunamadı",
                        data = new List<object>()
                    });
                }

                _logger.LogInformation("✅ API: Order details fetched: {Count} items", result.Data.Count);

                return Ok(new
                {
                    success = true,
                    data = result.Data
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ API: Error fetching order details for OrderId: {OrderId}", orderId);
                return Ok(new
                {
                    success = false,
                    message = $"Hata: {ex.Message}",
                    data = new List<object>()
                });
            }
        }
    }
}
