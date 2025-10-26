using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TSoftApiClient.Models;

namespace TSoftApiClient.Services
{
    /// <summary>
    /// T-Soft REST API Client - Supports both V3 and REST1 APIs
    /// COMPLETE VERSION - ALL METHODS IN ONE FILE
    /// </summary>
    public class TSoftApiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _token;
        private readonly string _baseUrl;
        private readonly ILogger<TSoftApiService> _logger;
        private readonly bool _debug;

        public TSoftApiService(HttpClient httpClient, IConfiguration config, ILogger<TSoftApiService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;

            _token = config["TSoftApi:Token"]
                ?? throw new InvalidOperationException("T-Soft API Token is not configured");

            _baseUrl = (config["TSoftApi:BaseUrl"] ?? "https://wawtesettur.tsoft.biz/rest1").TrimEnd('/');
            _debug = config["TSoftApi:Debug"] == "true";
        }

        // ========== REST1 API (Form-URLEncoded POST) ==========

        private async Task<(bool success, string body, int status)> Rest1PostAsync(
            string path,
            Dictionary<string, string> formData,
            CancellationToken ct = default)
        {
            try
            {
                var url = _baseUrl + (path.StartsWith('/') ? "" : "/") + path;
                var allData = new Dictionary<string, string>(formData) { ["token"] = _token };

                using var req = new HttpRequestMessage(HttpMethod.Post, url);
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
                req.Headers.Add("X-Auth-Token", _token);
                req.Headers.Accept.ParseAdd("application/json, text/plain, */*");
                req.Content = new FormUrlEncodedContent(allData);
                req.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(
                    "application/x-www-form-urlencoded")
                { CharSet = "UTF-8" };

                if (_debug)
                {
                    var formStr = string.Join("&", allData.Select(kv => $"{kv.Key}={kv.Value}"));
                    _logger.LogDebug("🟢 POST {Url} Form: {Form}", url, formStr);
                }

                using var resp = await _httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
                var body = await resp.Content.ReadAsStringAsync(ct);

                if (_debug)
                {
                    _logger.LogDebug("📊 Response: {Status} {Body}",
                        (int)resp.StatusCode,
                        body.Length > 500 ? body.Substring(0, 500) + "..." : body);
                }

                return (resp.IsSuccessStatusCode, body, (int)resp.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "REST1 POST failed: {Path}", path);
                return (false, "", 0);
            }
        }

        // ========== V3 API (JSON GET/POST) ==========

        private async Task<(bool success, string body, int status)> V3GetAsync(
            string path,
            Dictionary<string, string>? queryParams = null,
            CancellationToken ct = default)
        {
            try
            {
                var url = _baseUrl + (path.StartsWith('/') ? "" : "/") + path;

                if (queryParams is { Count: > 0 })
                {
                    var qs = string.Join("&", queryParams.Select(kvp =>
                        $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));
                    url += "?" + qs;
                }

                using var req = new HttpRequestMessage(HttpMethod.Get, url);
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
                req.Headers.Accept.ParseAdd("application/json");

                if (_debug) _logger.LogDebug("🔵 GET {Url}", url);

                using var resp = await _httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
                var body = await resp.Content.ReadAsStringAsync(ct);

                if (_debug) _logger.LogDebug("📊 Response: {Status}", (int)resp.StatusCode);

                return (resp.IsSuccessStatusCode, body, (int)resp.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "V3 GET failed: {Path}", path);
                return (false, "", 0);
            }
        }

        private async Task<(bool success, string body, int status)> V3PostAsync(
            string path,
            object jsonBody,
            CancellationToken ct = default)
        {
            try
            {
                var url = _baseUrl + (path.StartsWith('/') ? "" : "/") + path;

                var json = JsonSerializer.Serialize(jsonBody, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });

                using var req = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };

                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
                req.Headers.Accept.ParseAdd("application/json");

                if (_debug) _logger.LogDebug("🟢 POST {Url} JSON: {Json}", url, json);

                using var resp = await _httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
                var body = await resp.Content.ReadAsStringAsync(ct);

                if (_debug) _logger.LogDebug("📊 Response: {Status}", (int)resp.StatusCode);

                return (resp.IsSuccessStatusCode, body, (int)resp.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "V3 POST failed: {Path}", path);
                return (false, "", 0);
            }
        }

        // ========== PARSING ==========

        private TSoftApiResponse<T> ParseResponse<T>(string body)
        {
            if (string.IsNullOrWhiteSpace(body))
            {
                return new TSoftApiResponse<T>
                {
                    Success = false,
                    Message = new() { new() { Text = new() { "Empty response" } } }
                };
            }

            _logger.LogDebug("🔍 Parsing response, length: {Length}", body.Length);

            // CRITICAL: T-Soft API mixes string and number types!
            // Example: "SellingPrice":"272.72727273" (string) but "SellingPriceVatIncluded":300.00000000299997 (number)
            // We need VERY lenient parsing
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString |
                                 System.Text.Json.Serialization.JsonNumberHandling.WriteAsString |
                                 System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
                ReadCommentHandling = System.Text.Json.JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            };

            // Add custom converter for flexible number/string handling
            jsonOptions.Converters.Add(new FlexibleStringConverter());

            try
            {
                var wrapped = JsonSerializer.Deserialize<TSoftApiResponse<T>>(body, jsonOptions);
                if (wrapped != null && wrapped.Data != null)
                {
                    _logger.LogDebug("✅ Wrapped format parsed successfully");
                    return wrapped;
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "⚠️ Wrapped format parse failed: {Message}", ex.Message);
            }

            // ✅ TRY ARRAY FORMAT: {"data": [{...}]} - Eğer data array ise ilk elemanı al
            try
            {
                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty("data", out var dataElement) && dataElement.ValueKind == JsonValueKind.Array)
                {
                    var arrayLength = dataElement.GetArrayLength();
                    if (arrayLength > 0)
                    {
                        var firstElement = dataElement[0];
                        var item = JsonSerializer.Deserialize<T>(firstElement.GetRawText(), jsonOptions);
                        if (item != null)
                        {
                            _logger.LogDebug("✅ Array format parsed successfully (took first element from {Count} items)", arrayLength);

                            // success ve message alanlarını da al
                            var success = doc.RootElement.TryGetProperty("success", out var successProp) && successProp.GetBoolean();

                            return new TSoftApiResponse<T>
                            {
                                Success = success,
                                Data = item,
                                Message = null
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "⚠️ Array format parse failed: {Message}", ex.Message);
            }

            try
            {
                var direct = JsonSerializer.Deserialize<T>(body, jsonOptions);
                if (direct != null)
                {
                    _logger.LogDebug("✅ Direct format parsed successfully");
                    return new TSoftApiResponse<T> { Success = true, Data = direct };
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "⚠️ Direct format parse failed: {Message}", ex.Message);
            }

            try
            {
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;

                if (root.TryGetProperty("data", out var dataElement))
                {
                    _logger.LogDebug("📦 Found 'data' property, extracting...");
                    var data = JsonSerializer.Deserialize<T>(dataElement.GetRawText(), jsonOptions);

                    if (data != null)
                    {
                        _logger.LogDebug("✅ Data property parsed successfully");
                        var success = root.TryGetProperty("success", out var successElement)
                            ? successElement.GetBoolean()
                            : true;

                        return new TSoftApiResponse<T> { Success = success, Data = data };
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "⚠️ Data extraction parse failed: {Message}", ex.Message);
            }

            _logger.LogError("❌ ALL PARSING FAILED. Raw response: {Body}",
                body.Length > 1000 ? body.Substring(0, 1000) + "..." : body);

            return new TSoftApiResponse<T>
            {
                Success = false,
                Message = new() { new() { Text = new() { $"Failed to parse response. Length: {body.Length}" } } }
            };
        }

        /// <summary>
        /// Custom JSON converter that accepts both string and number for string properties
        /// Handles T-Soft's inconsistent API responses
        /// </summary>
        // Services/TSoftApiService.cs içindeki FlexibleStringConverter sınıfını değiştir:

        private class FlexibleStringConverter : System.Text.Json.Serialization.JsonConverter<string>
        {
            public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType == JsonTokenType.String)
                {
                    return reader.GetString();
                }
                else if (reader.TokenType == JsonTokenType.Number)
                {
                    if (reader.TryGetInt64(out var longValue))
                        return longValue.ToString();
                    if (reader.TryGetDouble(out var doubleValue))
                        return doubleValue.ToString(System.Globalization.CultureInfo.InvariantCulture);
                }
                else if (reader.TokenType == JsonTokenType.True)
                {
                    return "true";
                }
                else if (reader.TokenType == JsonTokenType.False)
                {
                    return "false";
                }
                else if (reader.TokenType == JsonTokenType.Null)
                {
                    return null;
                }
                else if (reader.TokenType == JsonTokenType.StartObject || reader.TokenType == JsonTokenType.StartArray)
                {
                    // Object veya Array geldiğinde skip et ve null dön
                    reader.Skip();
                    return null;
                }

                // Fallback
                try
                {
                    return reader.GetString();
                }
                catch
                {
                    return null;
                }
            }

            public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
            {
                writer.WriteStringValue(value);
            }
        }

        // ========== PRODUCT OPERATIONS ==========

        public async Task<TSoftApiResponse<List<Product>>> GetProductsAsync(
            int limit = 50,
            int page = 1,
            string? search = null,
            Dictionary<string, string>? filters = null,
            CancellationToken ct = default)
        {
            var form = new Dictionary<string, string> { ["limit"] = limit.ToString() };
            if (filters != null) foreach (var kv in filters) form[kv.Key] = kv.Value;

            var rest1Endpoints = new[] { "/product/getProducts", "/product/get", "/products/get" };

            foreach (var endpoint in rest1Endpoints)
            {
                var (success, body, _) = await Rest1PostAsync(endpoint, form, ct);
                if (success)
                {
                    _logger.LogInformation("✅ REST1 endpoint succeeded: {Endpoint}", endpoint);
                    return ParseResponse<List<Product>>(body);
                }
                _logger.LogDebug("⚠️ REST1 endpoint failed: {Endpoint}", endpoint);
            }

            var queryParams = new Dictionary<string, string>
            {
                ["page"] = page.ToString(),
                ["limit"] = limit.ToString()
            };
            if (!string.IsNullOrWhiteSpace(search)) queryParams["search"] = search;
            if (filters != null) foreach (var kv in filters) queryParams[kv.Key] = kv.Value;

            var v3Endpoints = new[] { "/catalog/products", "/api/v3/catalog/products" };
            foreach (var endpoint in v3Endpoints)
            {
                var (success, body, _) = await V3GetAsync(endpoint, queryParams, ct);
                if (success)
                {
                    _logger.LogInformation("✅ V3 endpoint succeeded: {Endpoint}", endpoint);
                    return ParseResponse<List<Product>>(body);
                }
            }

            return new TSoftApiResponse<List<Product>>
            {
                Success = false,
                Message = new() { new() { Text = new() { "All product endpoints failed" } } }
            };
        }

        public async Task<TSoftApiResponse<Product>> AddProductAsync(
            string code,
            string name,
            string categoryCode,
            decimal price,
            int stock = 0,
            Dictionary<string, string>? extraFields = null,
            CancellationToken ct = default)
        {
            var categoryId = int.TryParse(categoryCode.TrimStart('T', 't'), out var id) ? id : 1;

            var productV3 = new
            {
                name,
                wsProductCode = code,
                priceSale = price,
                stock,
                vat = extraFields?.TryGetValue("Vat", out var vatStr) == true ? int.Parse(vatStr) : 18,
                visibility = true,
                relation_hierarchy = new[] { new { id = categoryId, type = "category" } }
            };

            var v3Endpoints = new[] { "/catalog/products", "/api/v3/catalog/products" };
            foreach (var endpoint in v3Endpoints)
            {
                var (success, body, _) = await V3PostAsync(endpoint, productV3, ct);
                if (success) return ParseResponse<Product>(body);
            }

            var productData = new Dictionary<string, string>
            {
                ["ProductCode"] = code,
                ["ProductName"] = name,
                ["DefaultCategoryCode"] = categoryCode,
                ["SellingPrice"] = price.ToString("F2"),
                ["Stock"] = stock.ToString(),
                ["IsActive"] = "1"
            };
            if (extraFields != null) foreach (var kv in extraFields) productData[kv.Key] = kv.Value;

            var rest1Endpoints = new[] { "/product/createProducts", "/product/create", "/product/add" };
            foreach (var endpoint in rest1Endpoints)
            {
                var formData = new Dictionary<string, string>
                {
                    ["data"] = JsonSerializer.Serialize(new[] { productData })
                };
                var (success, body, _) = await Rest1PostAsync(endpoint, formData, ct);
                if (success) return ParseResponse<Product>(body);
            }

            return new TSoftApiResponse<Product>
            {
                Success = false,
                Message = new() { new() { Text = new() { "All product creation endpoints failed" } } }
            };
        }

        public async Task<TSoftApiResponse<object>> CreateProductsAsync(List<Product> products, CancellationToken ct = default)
        {
            var ok = new List<object>();
            var fail = new List<object>();

            foreach (var p in products)
            {
                var r = await AddProductAsync(
                    p.ProductCode ?? "",
                    p.ProductName ?? "",
                    p.DefaultCategoryCode ?? "T1",
                    decimal.TryParse(p.SellingPrice ?? p.Price, out var price) ? price : 0,
                    int.TryParse(p.Stock, out var stock) ? stock : 0,
                    null,
                    ct
                );

                if (r.Success) ok.Add(r.Data!);
                else fail.Add(new { p.ProductCode, r.Message });
            }

            return new TSoftApiResponse<object>
            {
                Success = fail.Count == 0,
                Data = new { success = ok.Count, failed = fail.Count, ok, fail }
            };
        }

        // ========== CATEGORY OPERATIONS ==========

        public async Task<TSoftApiResponse<List<Category>>> GetCategoriesAsync(CancellationToken ct = default)
        {
            var rest1Endpoints = new[] { "/category/getCategories", "/category/get", "/categories/get" };
            foreach (var endpoint in rest1Endpoints)
            {
                var (success, body, _) = await Rest1PostAsync(endpoint, new Dictionary<string, string>(), ct);
                if (success) return ParseResponse<List<Category>>(body);
            }

            var v3Endpoints = new[] { "/catalog/categories", "/api/v3/catalog/categories" };
            foreach (var endpoint in v3Endpoints)
            {
                var (success, body, _) = await V3GetAsync(endpoint, null, ct);
                if (success) return ParseResponse<List<Category>>(body);
            }

            return new TSoftApiResponse<List<Category>>
            {
                Success = false,
                Message = new() { new() { Text = new() { "All category endpoints failed" } } }
            };
        }

        public async Task<TSoftApiResponse<List<Category>>> GetCategoryTreeAsync(CancellationToken ct = default)
        {
            var (success, body, _) = await Rest1PostAsync("/category/getCategoryTree", new Dictionary<string, string>(), ct);

            if (success)
            {
                var parsed = ParseResponse<List<Category>>(body);
                if (parsed.Success && parsed.Data != null) BuildCategoryPaths(parsed.Data);
                return parsed;
            }

            var flatCategories = await GetCategoriesAsync(ct);
            if (flatCategories.Success && flatCategories.Data != null)
            {
                var tree = BuildTreeFromFlatList(flatCategories.Data);
                BuildCategoryPaths(tree);
                return new TSoftApiResponse<List<Category>> { Success = true, Data = tree };
            }

            return new TSoftApiResponse<List<Category>>
            {
                Success = false,
                Message = new() { new() { Text = new() { "Category tree failed" } } }
            };
        }

        private List<Category> BuildTreeFromFlatList(List<Category> flatList)
        {
            var categoryDict = flatList.ToDictionary(c => c.CategoryCode ?? "", c => c);
            var rootCategories = new List<Category>();

            foreach (var category in flatList)
            {
                category.Children = new List<Category>();
                if (string.IsNullOrEmpty(category.ParentCategoryCode))
                {
                    rootCategories.Add(category);
                }
                else if (categoryDict.TryGetValue(category.ParentCategoryCode, out var parent))
                {
                    if (parent.Children == null) parent.Children = new List<Category>();
                    parent.Children.Add(category);
                }
                else
                {
                    rootCategories.Add(category);
                }
            }
            return rootCategories;
        }

        private void BuildCategoryPaths(List<Category> categories, string parentPath = "")
        {
            foreach (var category in categories)
            {
                category.Path = string.IsNullOrEmpty(parentPath)
                    ? category.CategoryName ?? category.CategoryCode ?? "Unknown"
                    : $"{parentPath} > {category.CategoryName ?? category.CategoryCode}";

                if (category.Children != null && category.Children.Count > 0)
                    BuildCategoryPaths(category.Children, category.Path);
            }
        }

        // ========== PRODUCT IMAGES & ENHANCED ==========

        public async Task<TSoftApiResponse<List<ProductImage>>> GetProductImagesAsync(string productCode, CancellationToken ct = default)
        {
            var form = new Dictionary<string, string> { ["ProductCode"] = productCode };
            var (success, body, _) = await Rest1PostAsync("/product/getProductImages", form, ct);

            if (success) return ParseResponse<List<ProductImage>>(body);

            return new TSoftApiResponse<List<ProductImage>> { Success = true, Data = new List<ProductImage>() };
        }

        public async Task<Dictionary<string, List<ProductImage>>> GetBulkProductImagesAsync(
            List<string> productCodes, int maxParallel = 5, CancellationToken ct = default)
        {
            var result = new Dictionary<string, List<ProductImage>>();
            var semaphore = new SemaphoreSlim(maxParallel);

            var tasks = productCodes.Select(async code =>
            {
                await semaphore.WaitAsync(ct);
                try
                {
                    var images = await GetProductImagesAsync(code, ct);
                    if (images.Success && images.Data != null)
                        lock (result) { result[code] = images.Data; }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed to get images for product {Code}", code);
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(tasks);
            return result;
        }

        public async Task<TSoftApiResponse<List<Product>>> GetEnhancedProductsAsync(
            int limit = 50, int page = 1, bool includeImages = true, CancellationToken ct = default)
        {
            var productsResult = await GetProductsAsync(limit, page, null, null, ct);
            if (!productsResult.Success || productsResult.Data == null) return productsResult;

            var products = productsResult.Data;
            var categoryTreeResult = await GetCategoryTreeAsync(ct);
            var categoryDict = new Dictionary<string, Category>();

            if (categoryTreeResult.Success && categoryTreeResult.Data != null)
                FlattenCategoryTree(categoryTreeResult.Data, categoryDict);

            foreach (var product in products)
            {
                if (!string.IsNullOrEmpty(product.DefaultCategoryCode) &&
                    categoryDict.TryGetValue(product.DefaultCategoryCode, out var category))
                {
                    product.CategoryName = category.CategoryName;
                    product.CategoryPath = category.Path?.Split(" > ").ToList();
                }
            }

            if (includeImages && page == 1 && products.Count > 0)
            {
                var productCodes = products.Take(20).Select(p => p.ProductCode ?? "").Where(c => !string.IsNullOrEmpty(c)).ToList();
                var imagesDict = await GetBulkProductImagesAsync(productCodes, 3, ct);

                foreach (var product in products.Take(20))
                {
                    if (!string.IsNullOrEmpty(product.ProductCode) && imagesDict.TryGetValue(product.ProductCode, out var images) && images.Count > 0)
                    {
                        product.Images = images;
                        var primaryImage = images.FirstOrDefault(i =>
                            i.IsPrimary == "1" || i.IsMain == "1" || i.IsMain?.ToLower() == "true" || i.IsPrimary?.ToLower() == "true");

                        if (primaryImage != null)
                        {
                            product.ThumbnailUrl = primaryImage.ThumbnailUrl ?? primaryImage.Thumbnail ?? primaryImage.ImageUrl;
                            product.ImageUrl = primaryImage.ImageUrl ?? primaryImage.Image;
                        }
                        else if (images.Count > 0)
                        {
                            product.ThumbnailUrl = images[0].ThumbnailUrl ?? images[0].Thumbnail ?? images[0].ImageUrl;
                            product.ImageUrl = images[0].ImageUrl ?? images[0].Image;
                        }
                    }
                }
            }

            return new TSoftApiResponse<List<Product>> { Success = true, Data = products };
        }

        private void FlattenCategoryTree(List<Category> categories, Dictionary<string, Category> dict)
        {
            foreach (var category in categories)
            {
                if (!string.IsNullOrEmpty(category.CategoryCode)) dict[category.CategoryCode] = category;
                if (category.Children != null && category.Children.Count > 0)
                    FlattenCategoryTree(category.Children, dict);
            }
        }

        // ========== CUSTOMER, ORDER, ETC ==========

        public async Task<TSoftApiResponse<List<Customer>>> GetCustomersAsync(int limit = 50, Dictionary<string, string>? filters = null, CancellationToken ct = default)
        {
            var form = new Dictionary<string, string> { ["limit"] = limit.ToString() };
            if (filters != null) foreach (var kv in filters) form[kv.Key] = kv.Value;

            var rest1Endpoints = new[] { "/customer/getCustomers", "/customer/get", "/customers/get" };
            foreach (var endpoint in rest1Endpoints)
            {
                var (success, body, _) = await Rest1PostAsync(endpoint, form, ct);
                if (success) return ParseResponse<List<Customer>>(body);
            }

            var v3Endpoints = new[] { "/customers", "/api/v3/customers" };
            foreach (var endpoint in v3Endpoints)
            {
                var (success, body, _) = await V3GetAsync(endpoint, form, ct);
                if (success) return ParseResponse<List<Customer>>(body);
            }

            return new TSoftApiResponse<List<Customer>>
            {
                Success = false,
                Message = new() { new() { Text = new() { "All customer endpoints failed" } } }
            };
        }

        public async Task<TSoftApiResponse<List<Order>>> GetOrdersAsync(int limit = 50, Dictionary<string, string>? filters = null, CancellationToken ct = default)
        {
            var form = new Dictionary<string, string> { ["limit"] = limit.ToString() };
            if (filters != null) foreach (var kv in filters) form[kv.Key] = kv.Value;

            var rest1Endpoints = new[] { "/order/getOrders", "/order/get", "/orders/get" };
            foreach (var endpoint in rest1Endpoints)
            {
                var (success, body, _) = await Rest1PostAsync(endpoint, form, ct);
                if (success) return ParseResponse<List<Order>>(body);
            }

            var v3Endpoints = new[] { "/orders", "/api/v3/orders" };
            foreach (var endpoint in v3Endpoints)
            {
                var (success, body, _) = await V3GetAsync(endpoint, form, ct);
                if (success) return ParseResponse<List<Order>>(body);
            }

            return new TSoftApiResponse<List<Order>>
            {
                Success = false,
                Message = new() { new() { Text = new() { "All order endpoints failed" } } }
            };
        }

        public async Task<TSoftApiResponse<List<OrderDetail>>> GetOrderDetailsByOrderIdAsync(int orderId, CancellationToken ct = default)
        {
            var form = new Dictionary<string, string>
            {
                ["OrderId"] = orderId.ToString(),
                ["orderId"] = orderId.ToString()
            };

            var rest1Endpoints = new[] { "/order/getOrderDetailsByOrderId", "/order/getOrderDetails", "/order/details", "/orders/details" };
            foreach (var endpoint in rest1Endpoints)
            {
                var (success, body, _) = await Rest1PostAsync(endpoint, form, ct);
                if (success)
                {
                    var parsed = ParseResponse<List<OrderDetail>>(body);
                    if (parsed.Success && parsed.Data != null) return parsed;
                }
            }

            return new TSoftApiResponse<List<OrderDetail>>
            {
                Success = false,
                Message = new() { new() { Text = new() { "Order details endpoint failed" } } }
            };
        }

        public async Task<TSoftApiResponse<List<PaymentType>>> GetPaymentTypesAsync(CancellationToken ct = default)
        {
            var rest1Endpoints = new[] { "/order/getPaymentTypeList", "/payment/getTypes", "/paymenttype/get" };
            foreach (var endpoint in rest1Endpoints)
            {
                var (success, body, _) = await Rest1PostAsync(endpoint, new Dictionary<string, string>(), ct);
                if (success) return ParseResponse<List<PaymentType>>(body);
            }

            return new TSoftApiResponse<List<PaymentType>>
            {
                Success = false,
                Message = new() { new() { Text = new() { "All payment type endpoints failed" } } }
            };
        }

        public async Task<TSoftApiResponse<List<CargoCompany>>> GetCargoCompaniesAsync(CancellationToken ct = default)
        {
            var rest1Endpoints = new[] { "/order/getCargoCompanyList", "/cargo/getCompanies", "/cargocompany/get" };
            foreach (var endpoint in rest1Endpoints)
            {
                var (success, body, _) = await Rest1PostAsync(endpoint, new Dictionary<string, string>(), ct);
                if (success) return ParseResponse<List<CargoCompany>>(body);
            }

            return new TSoftApiResponse<List<CargoCompany>>
            {
                Success = false,
                Message = new() { new() { Text = new() { "All cargo company endpoints failed" } } }
            };
        }

        public async Task<TSoftApiResponse<List<OrderStatusInfo>>> GetOrderStatusListAsync(CancellationToken ct = default)
        {
            var rest1Endpoints = new[] { "/order/getOrderStatusList", "/orderstatus/get", "/order/statuses" };
            foreach (var endpoint in rest1Endpoints)
            {
                var (success, body, _) = await Rest1PostAsync(endpoint, new Dictionary<string, string>(), ct);
                if (success) return ParseResponse<List<OrderStatusInfo>>(body);
            }

            return new TSoftApiResponse<List<OrderStatusInfo>>
            {
                Success = false,
                Message = new() { new() { Text = new() { "All order status endpoints failed" } } }
            };
        }

        public async Task<TSoftApiResponse<Customer>> GetCustomerByIdAsync(int customerId, CancellationToken ct = default)
        {
            var form = new Dictionary<string, string>
            {
                ["CustomerId"] = customerId.ToString(),
                ["customerId"] = customerId.ToString(),
                ["Id"] = customerId.ToString()
            };

            var rest1Endpoints = new[] { "/customer/getCustomerById", "/customer/get", "/customers/get" };
            foreach (var endpoint in rest1Endpoints)
            {
                var (success, body, _) = await Rest1PostAsync(endpoint, form, ct);
                if (success) return ParseResponse<Customer>(body);
            }

            return new TSoftApiResponse<Customer>
            {
                Success = false,
                Message = new() { new() { Text = new() { "All customer endpoints failed" } } }
            };
        }

        // ========== 🎨 VARYANT YÖNETİMİ ==========

        /// <summary>
        /// Tek bir ürünün tüm varyantlarını (renk-beden) çeker
        /// </summary>
        public async Task<TSoftApiResponse<Product>> GetProductWithVariantsAsync(
            string productCode,
            CancellationToken ct = default)
        {
            _logger.LogInformation("🎨 Fetching product with variants: {Code}", productCode);

            // ✅ T2429 → 2429 dönüşümü (T-Soft bazen T olmadan bekliyor)
            var numericCode = productCode?.TrimStart('T', 't');

            var rest1Endpoints = new[]
            {
                "/product/get",
                "/product/getProduct",
                "/product/getProductDetail",
                "/product/detail"
            };

            // ✅ HEM T2429 HEM 2429 deneyelim
            var codesToTry = new List<string> { productCode };
            if (!string.IsNullOrEmpty(numericCode) && numericCode != productCode)
            {
                codesToTry.Add(numericCode);
            }

            foreach (var codeToTry in codesToTry)
            {
                _logger.LogDebug("🔄 Trying with code: {Code}", codeToTry);

                // ✅ Çalışan JavaScript kodundaki gibi TÜM bayrakları ekle
                var form = new Dictionary<string, string>
                {
                    // Ürün ID/Code (hem orijinal hem numeric)
                    ["ProductId"] = codeToTry,
                    ["productId"] = codeToTry,
                    ["ProductCode"] = productCode,  // Orijinal ProductCode
                    ["productCode"] = productCode,
                    ["code"] = codeToTry,
                    ["Id"] = codeToTry,

                    // Varyant bayrakları (TÜM VARYASYONLARı)
                    ["FetchDetails"] = "1",
                    ["FetchSubProducts"] = "1",
                    ["WithSubProducts"] = "1",
                    ["WithVariants"] = "1",
                    ["IncludeSubProducts"] = "1",
                    ["includeVariants"] = "1",
                    ["includeSubProducts"] = "1",
                    ["withVariants"] = "true",
                    ["fetchDetails"] = "true",

                    // Columns (JavaScript'teki gibi)
                    ["columns"] = "ProductId,ProductName,Name,ProductCode,Barcode,Stock,ModelCode",
                    ["start"] = "0",
                    ["length"] = "1"
                };

                foreach (var endpoint in rest1Endpoints)
                {
                    var (success, body, _) = await Rest1PostAsync(endpoint, form, ct);
                    if (success)
                    {
                        _logger.LogDebug("✅ REST1 endpoint with code {Code} succeeded: {Endpoint}", codeToTry, endpoint);
                        var result = ParseResponse<Product>(body);

                        if (result.Success && result.Data != null)
                        {
                            _logger.LogInformation("📦 Product has {Count} variants", result.Data.Variants.Count);
                            return result;
                        }
                    }
                    _logger.LogDebug("⚠️ REST1 endpoint with code {Code} failed or empty: {Endpoint}", codeToTry, endpoint);
                }
            }

            var v3Endpoints = new[]
            {
                $"/catalog/products/{productCode}",
                $"/api/v3/catalog/products/{productCode}",
                $"/products/{productCode}"
            };

            var queryParams = new Dictionary<string, string>
            {
                ["includeVariants"] = "1",
                ["includeSubProducts"] = "1",
                ["expand"] = "variants,subProducts",
                ["FetchSubProducts"] = "1",
                ["WithVariants"] = "1"
            };

            foreach (var endpoint in v3Endpoints)
            {
                var (success, body, _) = await V3GetAsync(endpoint, queryParams, ct);
                if (success)
                {
                    _logger.LogDebug("✅ V3 endpoint with variants succeeded: {Endpoint}", endpoint);
                    var result = ParseResponse<Product>(body);

                    if (result.Success && result.Data != null)
                    {
                        _logger.LogInformation("📦 Product has {Count} variants", result.Data.Variants.Count);
                        return result;
                    }
                }
            }

            _logger.LogWarning("⚠️ All endpoints failed for product: {Code}", productCode);
            return new TSoftApiResponse<Product>
            {
                Success = false,
                Message = new() { new() { Text = new() { $"Product not found: {productCode}" } } }
            };
        }

        /// <summary>
        /// Varyant bazlı stok güncelleme
        /// </summary>
        public async Task<TSoftApiResponse<object>> UpdateVariantStockAsync(
            string productCode,
            string variantCode,
            int newStock,
            CancellationToken ct = default)
        {
            _logger.LogInformation("📊 Updating variant stock: {Product}/{Variant} = {Stock}",
                productCode, variantCode, newStock);

            var form = new Dictionary<string, string>
            {
                ["productCode"] = productCode,
                ["variantCode"] = variantCode,
                ["stock"] = newStock.ToString(),
                ["stockQuantity"] = newStock.ToString()
            };

            var rest1Endpoints = new[]
            {
                "/product/updateVariantStock",
                "/product/updateStock",
                "/stock/update"
            };

            foreach (var endpoint in rest1Endpoints)
            {
                var (success, body, _) = await Rest1PostAsync(endpoint, form, ct);
                if (success)
                {
                    _logger.LogInformation("✅ Variant stock updated successfully");
                    return new TSoftApiResponse<object> { Success = true };
                }
            }

            return new TSoftApiResponse<object>
            {
                Success = false,
                Message = new() { new() { Text = new() { "Failed to update variant stock" } } }
            };
        }

        /// <summary>
        /// Renklere göre gruplandırılmış varyantlar
        /// </summary>
        public Dictionary<string, List<ProductVariant>> GetVariantsByColor(Product product)
        {
            var grouped = new Dictionary<string, List<ProductVariant>>();

            foreach (var variant in product.Variants)
            {
                var color = variant.GetColor();
                if (string.IsNullOrEmpty(color)) color = "Varsayılan";

                if (!grouped.ContainsKey(color))
                    grouped[color] = new List<ProductVariant>();

                grouped[color].Add(variant);
            }

            return grouped;
        }

        /// <summary>
        /// Bedenlere göre gruplandırılmış varyantlar
        /// </summary>
        public Dictionary<string, List<ProductVariant>> GetVariantsBySize(Product product)
        {
            var grouped = new Dictionary<string, List<ProductVariant>>();

            foreach (var variant in product.Variants)
            {
                var size = variant.GetSize();
                if (string.IsNullOrEmpty(size)) size = "Tek Beden";

                if (!grouped.ContainsKey(size))
                    grouped[size] = new List<ProductVariant>();

                grouped[size].Add(variant);
            }

            return grouped;
        }
    }
}