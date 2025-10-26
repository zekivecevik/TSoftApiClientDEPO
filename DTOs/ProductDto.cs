namespace TSoftApiClient.DTOs
{
    /// <summary>
    /// Ürün ekleme için DTO
    /// </summary>
    public class CreateProductDto
    {
        public required string Code { get; set; }
        public required string Name { get; set; }
        public required string CategoryCode { get; set; }
        public decimal Price { get; set; }
        public int Stock { get; set; } = 0;
        public string? Brand { get; set; }
        public string? Vat { get; set; }
        public string? Currency { get; set; }
        public string? BuyingPrice { get; set; }
        public string? ShortDescription { get; set; }
    }

    /// <summary>
    /// Toplu ürün ekleme için DTO
    /// </summary>
    public class BulkCreateProductDto
    {
        public required List<CreateProductDto> Products { get; set; }
    }
}
