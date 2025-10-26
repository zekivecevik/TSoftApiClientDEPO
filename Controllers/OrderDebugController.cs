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
    }
}
