// Controllers/WarehouseMvcController.cs - TAMAMI
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TSoftApiClient.Services;
using TSoftApiClient.Models;

namespace TSoftApiClient.Controllers
{
    [Authorize]
    public class WarehouseMvcController : Controller
    {
        private readonly WarehouseService _warehouseService;
        private readonly TSoftApiService _tsoftService;
        private readonly ILogger<WarehouseMvcController> _logger;

        public WarehouseMvcController(
            WarehouseService warehouseService,
            TSoftApiService tsoftService,
            ILogger<WarehouseMvcController> logger)
        {
            _warehouseService = warehouseService;
            _tsoftService = tsoftService;
            _logger = logger;
        }

        [Route("/Warehouses")]
        public IActionResult Index()
        {
            var warehouses = _warehouseService.GetAllWarehouses();
            ViewBag.TotalStock = _warehouseService.GetTotalStockInAllWarehouses();

            var allStocks = _warehouseService.GetWarehouseStocks(0);
            ViewBag.UniqueProducts = allStocks.Select(s => s.Barcode).Distinct().Count();

            return View("~/Views/Warehouses/Index.cshtml", warehouses);
        }

        [Route("/Warehouses/Create")]
        [HttpGet]
        [Authorize(Roles = "Admin,Manager")]
        public IActionResult Create()
        {
            return View("~/Views/Warehouses/Create.cshtml");
        }

        [Route("/Warehouses/Create")]
        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        public IActionResult Create(Warehouse warehouse)
        {
            if (!ModelState.IsValid)
            {
                return View("~/Views/Warehouses/Create.cshtml", warehouse);
            }

            _warehouseService.CreateWarehouse(warehouse);
            TempData["Success"] = "Depo başarıyla oluşturuldu";
            return RedirectToAction("Index");
        }

        [Route("/Warehouses/Edit/{id}")]
        [HttpGet]
        [Authorize(Roles = "Admin,Manager")]
        public IActionResult Edit(int id)
        {
            var warehouse = _warehouseService.GetWarehouseById(id);
            if (warehouse == null)
            {
                TempData["Error"] = "Depo bulunamadı";
                return RedirectToAction("Index");
            }

            return View("~/Views/Warehouses/Edit.cshtml", warehouse);
        }

        [Route("/Warehouses/Edit/{id}")]
        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        public IActionResult Edit(int id, Warehouse warehouse)
        {
            if (!ModelState.IsValid)
            {
                return View("~/Views/Warehouses/Edit.cshtml", warehouse);
            }

            var result = _warehouseService.UpdateWarehouse(id, warehouse);
            if (result)
            {
                TempData["Success"] = "Depo başarıyla güncellendi";
            }
            else
            {
                TempData["Error"] = "Depo güncellenemedi";
            }

            return RedirectToAction("Index");
        }

        [Route("/Warehouses/Delete/{id}")]
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public IActionResult Delete(int id)
        {
            try
            {
                var stocks = _warehouseService.GetWarehouseStocks(id);
                if (stocks.Count > 0)
                {
                    TempData["Error"] = $"Bu depoda {stocks.Count} adet stok kaydı var. Önce stokları temizleyin veya transfer edin.";
                    return RedirectToAction("Index");
                }

                var result = _warehouseService.DeleteWarehouse(id);
                if (result)
                {
                    TempData["Success"] = "Depo başarıyla silindi";
                }
                else
                {
                    TempData["Error"] = "Depo bulunamadı";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Depo silme hatası");
                TempData["Error"] = "Bir hata oluştu: " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        [Route("/Warehouses/{warehouseId}/Stock")]
        public IActionResult Stock(int warehouseId)
        {
            var warehouse = _warehouseService.GetWarehouseById(warehouseId);
            if (warehouse == null)
            {
                TempData["Error"] = "Depo bulunamadı";
                return RedirectToAction("Index");
            }

            var stocks = _warehouseService.GetWarehouseStocks(warehouseId);
            ViewBag.Warehouse = warehouse;
            return View("~/Views/Warehouses/Stock.cshtml", stocks);
        }

        [Route("/Warehouses/Counting")]
        [HttpGet]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Counting()
        {
            var warehouses = _warehouseService.GetAllWarehouses();
            var products = await _tsoftService.GetProductsAsync(limit: 1000);

            ViewBag.Warehouses = warehouses;
            ViewBag.Products = products.Data ?? new List<Product>();

            return View("~/Views/Warehouses/Counting.cshtml");
        }

        [Route("/Warehouses/Counting")]
        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        public IActionResult AddStock([FromForm] int warehouseId, [FromForm] string barcode, [FromForm] int quantity)
        {
            try
            {
                var result = _warehouseService.AddStockByBarcode(warehouseId, barcode, quantity);

                if (result.Success)
                {
                    return Json(new { success = true, message = result.Message, stock = result.Stock });
                }

                return Json(new { success = false, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stok ekleme hatası");
                return Json(new { success = false, message = "Bir hata oluştu" });
            }
        }

        [Route("/Warehouses/Transfer")]
        [HttpGet]
        [Authorize(Roles = "Admin,Manager")]
        public IActionResult Transfer()
        {
            var warehouses = _warehouseService.GetAllWarehouses();
            ViewBag.Warehouses = warehouses;
            return View("~/Views/Warehouses/Transfer.cshtml");
        }

        [Route("/Warehouses/Transfer")]
        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        public IActionResult Transfer([FromForm] int fromWarehouseId, [FromForm] int toWarehouseId, [FromForm] string barcode, [FromForm] int quantity)
        {
            try
            {
                var result = _warehouseService.TransferStock(fromWarehouseId, toWarehouseId, barcode, quantity);

                if (result.Success)
                {
                    TempData["Success"] = result.Message;
                }
                else
                {
                    TempData["Error"] = result.Message;
                }

                return RedirectToAction("Transfer");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Transfer hatası");
                TempData["Error"] = "Bir hata oluştu";
                return RedirectToAction("Transfer");
            }
        }

        [Route("/Warehouses/BarcodeScanner")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> BarcodeScanner()
        {
            var warehouses = _warehouseService.GetAllWarehouses();
            var products = await _tsoftService.GetProductsAsync(limit: 1000);

            ViewBag.Warehouses = warehouses;
            ViewBag.Products = products.Data ?? new List<Product>();

            return View("~/Views/Warehouses/BarcodeScanner.cshtml");
        }

        [Route("/Warehouses/SearchBarcode")]
        [HttpPost]
        public async Task<IActionResult> SearchBarcode([FromForm] string barcode)
        {
            try
            {
                var products = await _tsoftService.GetProductsAsync(limit: 1000);
                var product = products.Data?.FirstOrDefault(p => p.Barcode == barcode);

                if (product != null)
                {
                    var stocks = _warehouseService.GetProductStocksInAllWarehouses(barcode);

                    return Json(new
                    {
                        success = true,
                        product = new
                        {
                            code = product.ProductCode,
                            name = product.ProductName,
                            barcode = product.Barcode,
                            price = product.SellingPrice
                        },
                        stocks
                    });
                }

                return Json(new { success = false, message = "Ürün bulunamadı" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Barkod arama hatası");
                return Json(new { success = false, message = "Bir hata oluştu" });
            }
        }

        [Route("/Warehouses/SyncProducts")]
        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> SyncProducts()
        {
            try
            {
                var products = await _tsoftService.GetProductsAsync(limit: 1000);

                if (!products.Success || products.Data == null)
                {
                    TempData["Error"] = "Ürünler yüklenemedi";
                    return RedirectToAction("Index");
                }

                var result = _warehouseService.SyncProductsToDefaultWarehouse(products.Data);

                if (result.Success > 0)
                {
                    TempData["Success"] = $"✅ {result.Success} ürün Ana Depoya eklendi. {result.Failed} hata.";
                }
                else
                {
                    TempData["Error"] = $"❌ Hiç ürün eklenemedi. Hatalar: {string.Join(", ", result.Errors.Take(3))}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ürün senkronizasyon hatası");
                TempData["Error"] = "Bir hata oluştu: " + ex.Message;
            }

            return RedirectToAction("Index");
        }
    }
}