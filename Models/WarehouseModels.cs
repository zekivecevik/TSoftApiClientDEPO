namespace TSoftApiClient.Models
{
    public class Warehouse
    {
        public int Id { get; set; }
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public string Location { get; set; } = "";
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
    }

    public class WarehouseStock
    {
        public int Id { get; set; }
        public int WarehouseId { get; set; }
        public string Barcode { get; set; } = "";
        public int Quantity { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}