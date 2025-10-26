using System.Text.Json.Serialization;

namespace TSoftApiClient.Models
{
    /// <summary>
    /// T-Soft API'den dönen genel yanıt yapısı
    /// </summary>
    public class TSoftApiResponse<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public List<MessageItem>? Message { get; set; }
    }

    public class MessageItem
    {
        public List<string>? Text { get; set; }
    }

    /// <summary>
    /// Ürün modeli - CRITICAL: TÜM ALANLAR STRING!
    /// T-Soft API numeric değerleri string olarak döndürüyor
    /// ✅ VARYANT DESTEĞİ EKLENDİ
    /// </summary>
    public class Product
    {
        // IDs - STRING
        public string? ProductId { get; set; }
        public string? ProductCode { get; set; }

        // Basic Info
        public string? ProductName { get; set; }
        public string? DefaultCategoryCode { get; set; }
        public string? DefaultCategoryId { get; set; }
        public string? DefaultCategoryName { get; set; }
        public string? DefaultCategoryPath { get; set; }

        // Stock - STRING
        public string? Stock { get; set; }
        public string? StockUnit { get; set; }
        public string? StockUnitId { get; set; }

        // Status
        public string? IsActive { get; set; }
        public string? IsApproved { get; set; }
        public string? ComparisonSites { get; set; }
        public string? HasSubProducts { get; set; }
        public string? HasImages { get; set; }

        // Prices - ALL STRING!
        public string? Price { get; set; }
        public string? BuyingPrice { get; set; }
        public string? SellingPrice { get; set; }
        public string? SellingPriceVatIncluded { get; set; }
        public string? SellingPriceVatIncludedNoDiscount { get; set; }
        public string? DiscountedSellingPrice { get; set; }
        public string? Vat { get; set; }
        public string? CurrencyId { get; set; }
        public string? Currency { get; set; }

        // Brand & Model
        public string? Brand { get; set; }
        public string? BrandId { get; set; }
        public string? BrandLink { get; set; }
        public string? Model { get; set; }
        public string? ModelId { get; set; }

        // Supplier
        public string? SupplierId { get; set; }
        public string? SupplierProductCode { get; set; }

        // Product Details
        public string? Barcode { get; set; }
        public string? Description { get; set; }
        public string? ShortDescription { get; set; }
        public string? SearchKeywords { get; set; }
        public string? SeoLink { get; set; }

        // Display Settings
        public string? DisplayOnHomepage { get; set; }
        public string? IsNewProduct { get; set; }
        public string? OnSale { get; set; }
        public string? IsDisplayProduct { get; set; }
        public string? VendorDisplayOnly { get; set; }
        public string? DisplayWithVat { get; set; }
        public string? CustomerGroupDisplay { get; set; }

        // Additional Fields
        public string? Additional1 { get; set; }
        public string? Additional2 { get; set; }
        public string? Additional3 { get; set; }

        // Images - STRING URLs
        public string? ImageUrl { get; set; }
        public string? ThumbnailUrl { get; set; }
        public string? Image { get; set; }
        public List<ProductImage>? Images { get; set; }

        // Category Info (for enhanced products)
        public string? CategoryName { get; set; }
        public List<string>? CategoryPath { get; set; }
        public List<string>? Categories { get; set; }

        // Dates - STRING
        public string? UpdateDate { get; set; }
        public string? UpdateDateTimeStamp { get; set; }
        public string? CreatedDate { get; set; }
        public string? DateCreated { get; set; }
        public string? LastModified { get; set; }

        // Other
        public string? StockCode { get; set; }

        // ========== 🎨 VARYANT DESTEĞİ (YENİ) ==========

        /// <summary>
        /// Alt ürünler / Varyantlar (Renk-Beden kombinasyonları)
        /// API'den "SubProducts" / "SubProductList" / "Products" alanlarından gelir
        /// </summary>
        [JsonPropertyName("subProducts")]
        public List<ProductVariant>? SubProducts { get; set; }

        [JsonPropertyName("subProductList")]
        public List<ProductVariant>? SubProductList { get; set; }

        [JsonPropertyName("products")]
        public List<ProductVariant>? ProductVariants { get; set; }

        /// <summary>
        /// Varyantları birleştirilmiş liste olarak döner
        /// </summary>
        [JsonIgnore]
        public List<ProductVariant> Variants
        {
            get
            {
                var variants = new List<ProductVariant>();
                if (SubProducts?.Count > 0) variants.AddRange(SubProducts);
                else if (SubProductList?.Count > 0) variants.AddRange(SubProductList);
                else if (ProductVariants?.Count > 0) variants.AddRange(ProductVariants);
                return variants;
            }
        }

        /// <summary>
        /// Bu ürünün varyantları var mı?
        /// </summary>
        [JsonIgnore]
        public bool HasVariants => Variants.Count > 0;
    }

    /// <summary>
    /// 🎨 Ürün Varyantı (Renk-Beden kombinasyonu)
    /// </summary>
    public class ProductVariant
    {
        // Basic Info
        public string? ProductCode { get; set; }
        public string? ProductId { get; set; }
        public string? SubProductId { get; set; }
        public string? VariantId { get; set; }
        public string? VariantCode { get; set; }
        public string? ProductName { get; set; }
        public string? VariantName { get; set; }
        public string? SubId { get; set; }
        public string? Id { get; set; }

        // ✅ Renk (JavaScript'teki TÜM olası alanlar)
        [JsonPropertyName("color")]
        public string? Color { get; set; }

        [JsonPropertyName("colour")]
        public string? Colour { get; set; }

        [JsonPropertyName("colorCode")]
        public string? ColorCode { get; set; }

        [JsonPropertyName("colorName")]
        public string? ColorName { get; set; }

        [JsonPropertyName("renk")]
        public string? Renk { get; set; }

        [JsonPropertyName("Property1")]
        public string? Property1 { get; set; }

        [JsonPropertyName("PropertyValue1")]
        public string? PropertyValue1 { get; set; }

        [JsonPropertyName("Variant1")]
        public string? Variant1 { get; set; }

        [JsonPropertyName("Attribute1")]
        public string? Attribute1 { get; set; }

        [JsonPropertyName("Option1")]
        public string? Option1 { get; set; }

        [JsonPropertyName("Nitelik1")]
        public string? Nitelik1 { get; set; }

        // ✅ Beden (JavaScript'teki TÜM olası alanlar)
        [JsonPropertyName("size")]
        public string? Size { get; set; }

        [JsonPropertyName("sizeCode")]
        public string? SizeCode { get; set; }

        [JsonPropertyName("sizeName")]
        public string? SizeName { get; set; }

        [JsonPropertyName("beden")]
        public string? Beden { get; set; }

        [JsonPropertyName("Property2")]
        public string? Property2 { get; set; }

        [JsonPropertyName("PropertyValue2")]
        public string? PropertyValue2 { get; set; }

        [JsonPropertyName("Variant2")]
        public string? Variant2 { get; set; }

        [JsonPropertyName("Attribute2")]
        public string? Attribute2 { get; set; }

        [JsonPropertyName("Option2")]
        public string? Option2 { get; set; }

        [JsonPropertyName("Nitelik2")]
        public string? Nitelik2 { get; set; }

        // Stok
        [JsonPropertyName("stock")]
        public string? Stock { get; set; }

        [JsonPropertyName("stockQuantity")]
        public string? StockQuantity { get; set; }

        [JsonPropertyName("availableStock")]
        public string? AvailableStock { get; set; }

        // Fiyat
        [JsonPropertyName("price")]
        public string? Price { get; set; }

        [JsonPropertyName("sellingPrice")]
        public string? SellingPrice { get; set; }

        [JsonPropertyName("buyingPrice")]
        public string? BuyingPrice { get; set; }

        // Durum
        [JsonPropertyName("isActive")]
        public string? IsActive { get; set; }

        [JsonPropertyName("isAvailable")]
        public string? IsAvailable { get; set; }

        // Barkod
        [JsonPropertyName("barcode")]
        public string? Barcode { get; set; }

        [JsonPropertyName("sku")]
        public string? Sku { get; set; }

        [JsonPropertyName("modelCode")]
        public string? ModelCode { get; set; }

        // Görsel
        [JsonPropertyName("image")]
        public string? Image { get; set; }

        [JsonPropertyName("imageUrl")]
        public string? ImageUrl { get; set; }

        [JsonPropertyName("thumbnail")]
        public string? Thumbnail { get; set; }

        [JsonPropertyName("thumbnailUrl")]
        public string? ThumbnailUrl { get; set; }

        // ========== HELPER METODLAR ==========

        /// <summary>
        /// Renk değerini döner (JavaScript kodundaki gibi TÜM olası alanları kontrol eder)
        /// </summary>
        public string GetColor() =>
            Color ?? Colour ?? ColorName ?? ColorCode ?? Renk ??
            Property1 ?? PropertyValue1 ?? Variant1 ?? Attribute1 ?? Option1 ?? Nitelik1 ?? "";

        /// <summary>
        /// Beden değerini döner (JavaScript kodundaki gibi TÜM olası alanları kontrol eder)
        /// </summary>
        public string GetSize() =>
            Size ?? SizeName ?? SizeCode ?? Beden ??
            Property2 ?? PropertyValue2 ?? Variant2 ?? Attribute2 ?? Option2 ?? Nitelik2 ?? "";

        /// <summary>
        /// Stok miktarını integer olarak döner
        /// </summary>
        public int GetStockQuantity()
        {
            var stockStr = Stock ?? StockQuantity ?? AvailableStock ?? "0";
            return int.TryParse(stockStr, out var qty) ? qty : 0;
        }

        /// <summary>
        /// Fiyatı decimal olarak döner
        /// </summary>
        public decimal GetPrice()
        {
            var priceStr = SellingPrice ?? Price ?? "0";
            return decimal.TryParse(priceStr,
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out var price) ? price : 0;
        }

        /// <summary>
        /// Varyant aktif mi?
        /// </summary>
        [JsonIgnore]
        public bool IsActiveVariant
        {
            get
            {
                if (string.IsNullOrEmpty(IsActive) && string.IsNullOrEmpty(IsAvailable))
                    return true;

                var activeValue = (IsActive ?? IsAvailable ?? "1").ToLower().Trim();
                return activeValue == "1" ||
                       activeValue == "true" ||
                       activeValue == "yes" ||
                       activeValue == "active";
            }
        }

        /// <summary>
        /// Varyant display adı (örn: "Kırmızı - 36")
        /// </summary>
        [JsonIgnore]
        public string DisplayName
        {
            get
            {
                var color = GetColor();
                var size = GetSize();

                if (!string.IsNullOrEmpty(color) && !string.IsNullOrEmpty(size))
                    return $"{color} - {size}";
                if (!string.IsNullOrEmpty(color))
                    return color;
                if (!string.IsNullOrEmpty(size))
                    return size;

                return VariantName ?? ProductName ?? VariantCode ?? ProductCode ?? "Varyant";
            }
        }
    }

    /// <summary>
    /// Ürün görseli modeli - CRITICAL: TÜM ALANLAR STRING!
    /// </summary>
    public class ProductImage
    {
        public string? ImageId { get; set; }
        public string? ProductImageId { get; set; }
        public string? ImageUrl { get; set; }
        public string? ImagePath { get; set; }
        public string? Image { get; set; }
        public string? ThumbnailUrl { get; set; }
        public string? Thumbnail { get; set; }
        public string? IsPrimary { get; set; }
        public string? IsMain { get; set; }
        public string? IsActive { get; set; }
        public string? Order { get; set; }
        public string? OrderNo { get; set; }
    }

    /// <summary>
    /// Kategori modeli
    /// </summary>
    public class Category
    {
        public string? CategoryCode { get; set; }
        public string? CategoryName { get; set; }
        public string? ParentCategoryCode { get; set; }
        public string? IsActive { get; set; }
        public string? CategoryId { get; set; }
        public string? ParentCategoryId { get; set; }
        public string? Level { get; set; }
        public string? Order { get; set; }
        public List<Category>? Children { get; set; }
        public string? Path { get; set; }
    }

    public class Customer
    {
        public string? CustomerId { get; set; }
        public string? CustomerCode { get; set; }
        public string? CustomerName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? IsActive { get; set; }
        public string? CreatedDate { get; set; }
        public string? DateCreated { get; set; }
        public string? UpdateDate { get; set; }
        public string? UpdateDateTimeStamp { get; set; }
        public string? LastModified { get; set; }
        public string? CustomerGroupId { get; set; }
        public string? CustomerGroup { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }
        public string? Address { get; set; }
    }

    public class Order
    {
        public string? Id { get; set; }
        public string? OrderId { get; set; }
        public string? OrderCode { get; set; }
        public string? Status { get; set; }
        public string? OrderStatus { get; set; }
        public string? OrderStatusId { get; set; }
        public string? SupplyStatus { get; set; }
        public string? CustomerId { get; set; }
        public string? CustomerCode { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerUsername { get; set; }
        public string? CustomerEmail { get; set; }
        public string? CustomerPhone { get; set; }
        public string? CustomerGroupId { get; set; }
        public string? OrderDate { get; set; }
        public string? OrderDateTimeStamp { get; set; }
        public string? CreatedDate { get; set; }
        public string? DateCreated { get; set; }
        public string? UpdateDate { get; set; }
        public string? UpdateDateTimeStamp { get; set; }
        public string? ApprovalTime { get; set; }
        public string? City { get; set; }
        public string? ShippingCity { get; set; }
        public string? ShippingAddress { get; set; }
        public string? BillingCity { get; set; }
        public string? Total { get; set; }
        public string? TotalAmount { get; set; }
        public string? OrderTotalPrice { get; set; }
        public string? OrderSubtotal { get; set; }
        public string? GeneralTotal { get; set; }
        public string? SubTotal { get; set; }
        public string? DiscountTotal { get; set; }
        public string? TaxTotal { get; set; }
        public string? ShippingTotal { get; set; }
        public string? Currency { get; set; }
        public string? SiteDefaultCurrency { get; set; }
        public string? PaymentTypeId { get; set; }
        public string? PaymentType { get; set; }
        public string? PaymentTypeName { get; set; }
        public string? SubPaymentTypeId { get; set; }
        public string? PaymentSubMethod { get; set; }
        public string? PaymentBankName { get; set; }
        public string? Bank { get; set; }
        public string? PaymentInfo { get; set; }
        public string? CargoId { get; set; }
        public string? CargoCode { get; set; }
        public string? Cargo { get; set; }
        public string? CargoCompanyId { get; set; }
        public string? CargoCompanyName { get; set; }
        public string? ShippingCompanyName { get; set; }
        public string? CargoTrackingCode { get; set; }
        public string? CargoPaymentMethod { get; set; }
        public string? CargoChargeWithVat { get; set; }
        public string? CargoChargeWithoutVat { get; set; }
        public string? Application { get; set; }
        public string? Language { get; set; }
        public string? ExchangeRate { get; set; }
        public string? Installment { get; set; }
        public string? IsTransferred { get; set; }
        public string? NonMemberShopping { get; set; }
        public string? WaybillNumber { get; set; }
        public string? InvoiceNumber { get; set; }
        public int ItemCount { get; set; }
        public List<OrderDetail>? OrderDetails { get; set; }
        public List<OrderDetail>? Items { get; set; }
    }

    public class OrderDetail
    {
        public string? Id { get; set; }
        public string? OrderId { get; set; }
        public string? ProductId { get; set; }
        public string? ProductCode { get; set; }
        public string? ProductName { get; set; }
        public string? Quantity { get; set; }
        public string? Price { get; set; }
        public string? Total { get; set; }
        public string? City { get; set; }
        public string? ShippingCity { get; set; }
        public string? SupplyStatus { get; set; }
    }

    public class OrderStatusInfo
    {
        public string? Id { get; set; }
        public string? OrderStatusId { get; set; }
        public string? Name { get; set; }
        public string? OrderStatusName { get; set; }
        public string? Code { get; set; }
    }

    public class PaymentType
    {
        public string? Id { get; set; }
        public string? PaymentTypeId { get; set; }
        public string? Name { get; set; }
        public string? PaymentTypeName { get; set; }
        public string? Code { get; set; }
    }

    public class CargoCompany
    {
        public string? Id { get; set; }
        public string? CargoCompanyId { get; set; }
        public string? Name { get; set; }
        public string? CargoCompanyName { get; set; }
        public string? Code { get; set; }
    }
}
