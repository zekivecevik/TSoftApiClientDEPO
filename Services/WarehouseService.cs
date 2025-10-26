using TSoftApiClient.Models;

namespace TSoftApiClient.Services
{
    public class WarehouseService
    {
        private readonly ILogger<WarehouseService> _logger;
        private static List<Warehouse> _warehouses = new();
        private static List<WarehouseStock> _stocks = new();
        private static int _nextWarehouseId = 1;
        private static int _nextStockId = 1;

        public WarehouseService(ILogger<WarehouseService> logger)
        {
            _logger = logger;
            InitializeDefaultWarehouses();
        }

        private void InitializeDefaultWarehouses()
        {
            if (_warehouses.Count == 0)
            {
                _warehouses.AddRange(new[]
                {
                    new Warehouse
                    {
                        Id = _nextWarehouseId++,
                        Code = "DEPO-01",
                        Name = "Ana Depo",
                        Location = "İstanbul",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new Warehouse
                    {
                        Id = _nextWarehouseId++,
                        Code = "DEPO-02",
                        Name = "Yedek Depo",
                        Location = "Ankara",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    }
                });
            }
        }

        public List<Warehouse> GetAllWarehouses()
        {
            return _warehouses.Where(w => w.IsActive).ToList();
        }

        public Warehouse? GetWarehouseById(int id)
        {
            return _warehouses.FirstOrDefault(w => w.Id == id);
        }

        public void CreateWarehouse(Warehouse warehouse)
        {
            warehouse.Id = _nextWarehouseId++;
            warehouse.CreatedAt = DateTime.UtcNow;
            warehouse.IsActive = true;
            _warehouses.Add(warehouse);

            _logger.LogInformation("✅ Depo oluşturuldu: {Code} - {Name}", warehouse.Code, warehouse.Name);
        }

        public bool UpdateWarehouse(int id, Warehouse warehouse)
        {
            var existing = GetWarehouseById(id);
            if (existing == null)
            {
                return false;
            }

            existing.Code = warehouse.Code;
            existing.Name = warehouse.Name;
            existing.Location = warehouse.Location;

            _logger.LogInformation("✅ Depo güncellendi: {Code} - {Name}", existing.Code, existing.Name);
            return true;
        }

        public bool DeleteWarehouse(int id)
        {
            var warehouse = GetWarehouseById(id);
            if (warehouse == null)
            {
                return false;
            }

            warehouse.IsActive = false;
            _logger.LogInformation("🗑️ Depo silindi: {Code} - {Name}", warehouse.Code, warehouse.Name);
            return true;
        }

        public List<WarehouseStock> GetWarehouseStocks(int warehouseId)
        {
            return _stocks.Where(s => s.WarehouseId == warehouseId).ToList();
        }

        public (bool Success, string Message, WarehouseStock? Stock) AddStockByBarcode(int warehouseId, string barcode, int quantity)
        {
            var warehouse = GetWarehouseById(warehouseId);
            if (warehouse == null)
            {
                return (false, "Depo bulunamadı", null);
            }

            var existingStock = _stocks.FirstOrDefault(s => s.WarehouseId == warehouseId && s.Barcode == barcode);

            if (existingStock != null)
            {
                existingStock.Quantity += quantity;
                existingStock.LastUpdated = DateTime.UtcNow;

                _logger.LogInformation("✅ Stok güncellendi: {Barcode} +{Quantity} = {Total} @ {Warehouse}",
                    barcode, quantity, existingStock.Quantity, warehouse.Name);

                return (true, $"Stok güncellendi: {existingStock.Quantity} adet", existingStock);
            }
            else
            {
                var newStock = new WarehouseStock
                {
                    Id = _nextStockId++,
                    WarehouseId = warehouseId,
                    Barcode = barcode,
                    Quantity = quantity,
                    CreatedAt = DateTime.UtcNow,
                    LastUpdated = DateTime.UtcNow
                };

                _stocks.Add(newStock);

                _logger.LogInformation("✅ Yeni stok eklendi: {Barcode} = {Quantity} @ {Warehouse}",
                    barcode, quantity, warehouse.Name);

                return (true, $"Yeni stok eklendi: {quantity} adet", newStock);
            }
        }

        public (bool Success, string Message) TransferStock(int fromWarehouseId, int toWarehouseId, string barcode, int quantity)
        {
            var fromWarehouse = GetWarehouseById(fromWarehouseId);
            var toWarehouse = GetWarehouseById(toWarehouseId);

            if (fromWarehouse == null || toWarehouse == null)
            {
                return (false, "Depo bulunamadı");
            }

            var fromStock = _stocks.FirstOrDefault(s => s.WarehouseId == fromWarehouseId && s.Barcode == barcode);

            if (fromStock == null || fromStock.Quantity < quantity)
            {
                return (false, "Yetersiz stok");
            }

            fromStock.Quantity -= quantity;
            fromStock.LastUpdated = DateTime.UtcNow;

            var toStock = _stocks.FirstOrDefault(s => s.WarehouseId == toWarehouseId && s.Barcode == barcode);

            if (toStock != null)
            {
                toStock.Quantity += quantity;
                toStock.LastUpdated = DateTime.UtcNow;
            }
            else
            {
                _stocks.Add(new WarehouseStock
                {
                    Id = _nextStockId++,
                    WarehouseId = toWarehouseId,
                    Barcode = barcode,
                    Quantity = quantity,
                    CreatedAt = DateTime.UtcNow,
                    LastUpdated = DateTime.UtcNow
                });
            }

            _logger.LogInformation("✅ Transfer tamamlandı: {Barcode} x{Quantity} | {From} → {To}",
                barcode, quantity, fromWarehouse.Name, toWarehouse.Name);

            return (true, $"{quantity} adet başarıyla transfer edildi");
        }

        public List<WarehouseStockInfo> GetProductStocksInAllWarehouses(string barcode)
        {
            return _stocks
                .Where(s => s.Barcode == barcode)
                .Select(s => new WarehouseStockInfo
                {
                    WarehouseName = GetWarehouseById(s.WarehouseId)?.Name ?? "Bilinmeyen",
                    Quantity = s.Quantity
                })
                .ToList();
        }
    }

    public class WarehouseStockInfo
    {
        public string WarehouseName { get; set; } = "";
        public int Quantity { get; set; }
    }
}