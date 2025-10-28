using Microsoft.AspNetCore.Mvc;
using TSoftApiClient.Services;
using System.Text.Json;

namespace TSoftApiClient.Controllers
{
    [Route("OrderDebug")]
    public class OrderDebugController : Controller
    {
        private readonly TSoftApiService _tsoftService;

        public OrderDebugController(TSoftApiService tsoftService)
        {
            _tsoftService = tsoftService;
        }

        public async Task<IActionResult> Index()
        {
            var result = await _tsoftService.GetOrdersAsync(limit: 1);

            if (result.Success && result.Data?.Count > 0)
            {
                var firstOrder = result.Data[0];
                var json = JsonSerializer.Serialize(firstOrder, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });

                ViewBag.OrderJson = json;
                ViewBag.OrderId = firstOrder.OrderId;
            }
            else
            {
                ViewBag.OrderJson = "No orders found or API failed";
            }

            return View();
        }

        /// <summary>
        /// Test order details API - Sipariş detaylarını çekmek için
        /// </summary>
        [Route("OrderDebug/TestDetails/{orderId}")]
        public async Task<IActionResult> TestDetails(int orderId)
        {
            ViewBag.OrderId = orderId;

            // Method 1: GetOrderDetailsByOrderId
            var result1 = await _tsoftService.GetOrderDetailsByOrderIdAsync(orderId);
            ViewBag.Method1Success = result1.Success;
            ViewBag.Method1Json = JsonSerializer.Serialize(result1, new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });

            return View();
        }

        /// <summary>
        /// Test all order details endpoints - COMPREHENSIVE TEST
        /// </summary>
        [Route("OrderDebug/TestAllDetailMethods")]
        public async Task<IActionResult> TestAllDetailMethods()
        {
            // Önce bir sipariş alalım
            var ordersResult = await _tsoftService.GetOrdersAsync(limit: 5);

            if (!ordersResult.Success || ordersResult.Data?.Count == 0)
            {
                ViewBag.Error = "Sipariş bulunamadı";
                return View();
            }

            var results = new List<object>();

            // İlk 3 siparişi test edelim
            foreach (var order in ordersResult.Data.Take(3))
            {
                if (!int.TryParse(order.OrderId, out var orderId))
                    continue;

                var testResult = new Dictionary<string, object>
                {
                    ["OrderId"] = orderId,
                    ["OrderCode"] = order.OrderCode ?? "N/A",
                    ["CustomerName"] = order.CustomerName ?? "N/A"
                };

                try
                {
                    var detailsResult = await _tsoftService.GetOrderDetailsByOrderIdAsync(orderId);

                    testResult["API_Success"] = detailsResult.Success;
                    testResult["ItemCount"] = detailsResult.Data?.Count ?? 0;

                    if (detailsResult.Success && detailsResult.Data != null && detailsResult.Data.Count > 0)
                    {
                        testResult["Status"] = "✅ SUCCESS";
                        testResult["FirstItem"] = new
                        {
                            ProductName = detailsResult.Data[0].ProductName,
                            Quantity = detailsResult.Data[0].Quantity,
                            City = detailsResult.Data[0].City,
                            SupplyStatus = detailsResult.Data[0].SupplyStatus
                        };
                    }
                    else
                    {
                        testResult["Status"] = "❌ FAILED";
                        testResult["Error"] = detailsResult.Message?.FirstOrDefault()?.Text?.FirstOrDefault() ?? "No data";
                    }
                }
                catch (Exception ex)
                {
                    testResult["Status"] = "💥 EXCEPTION";
                    testResult["Error"] = ex.Message;
                }

                results.Add(testResult);
            }

            ViewBag.Results = JsonSerializer.Serialize(new
            {
                TotalOrdersTested = results.Count,
                Results = results
            }, new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });

            return View();
        }

        /// <summary>
        /// Tek bir sipariş için detaylı test
        /// </summary>
        [Route("OrderDebug/TestSingleOrder/{orderId}")]
        public async Task<IActionResult> TestSingleOrder(int orderId)
        {
            ViewBag.OrderId = orderId;

            var result = await _tsoftService.GetOrderDetailsByOrderIdAsync(orderId);

            ViewBag.Results = JsonSerializer.Serialize(new
            {
                OrderId = orderId,
                Success = result.Success,
                DataCount = result.Data?.Count ?? 0,
                Message = result.Message?.FirstOrDefault()?.Text?.FirstOrDefault(),
                Data = result.Data?.Select(d => new
                {
                    d.ProductName,
                    d.Quantity,
                    d.Price,
                    d.City,
                    d.SupplyStatus,
                    d.ShippingCity
                })
            }, new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });

            return View("TestAllDetailMethods");
        }
    }
}