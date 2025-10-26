# T-Soft API Client - ASP.NET Core 9

Python projesinden ASP.NET Core 9'a dönüştürülmüş T-Soft REST1 API entegrasyonu.

## 🚀 Hızlı Başlangıç

### Gereksinimler
- Visual Studio 2022 veya Visual Studio Code
- .NET 9 SDK
- T-Soft API Token

### Kurulum

1. **Projeyi klonla veya aç**
   ```bash
   cd TSoftApiClient
   ```

2. **Bağımlılıkları yükle**
   ```bash
   dotnet restore
   ```

3. **appsettings.json dosyasını düzenle**
   ```json
   {
     "TSoftApi": {
       "Token": "SENIN_TOKEN_BURAYA",
       "BaseUrl": "https://wawtesettur.tsoft.biz/rest1"
     }
   }
   ```

4. **Projeyi çalıştır**
   ```bash
   dotnet run
   ```

   Uygulama şu adreste başlayacak: `http://localhost:5000`

5. **Swagger UI'ı aç**
   - Tarayıcıda: `http://localhost:5000`
   - Veya: `http://localhost:5000/swagger`

## 📚 API Endpoints

### 🏥 Health Check
```
GET /health
```

### 📦 Ürünler

#### Ürünleri Listele
```
GET /api/products?limit=50
```

#### Tek Ürün Ekle
```
POST /api/products
Content-Type: application/json

{
  "code": "PROD-001",
  "name": "Test Ürünü",
  "categoryCode": "T48",
  "price": 299.99,
  "stock": 100,
  "brand": "Test Marka",
  "vat": "18",
  "currency": "TL",
  "buyingPrice": "200",
  "shortDescription": "Harika bir ürün!"
}
```

#### Toplu Ürün Ekle
```
POST /api/products/bulk
Content-Type: application/json

{
  "products": [
    {
      "code": "BULK-001",
      "name": "Toplu Ürün 1",
      "categoryCode": "T48",
      "price": 199.99,
      "stock": 50
    },
    {
      "code": "BULK-002",
      "name": "Toplu Ürün 2",
      "categoryCode": "T48",
      "price": 249.99,
      "stock": 30
    }
  ]
}
```

### 📁 Kategoriler

#### Kategorileri Listele
```
GET /api/categories
```

## 🛠️ Visual Studio'da Çalıştırma

1. **Solution'ı aç**: `TSoftApiClient.sln` dosyasına çift tıkla
2. **F5** tuşuna bas veya **Start Debugging** butonuna tıkla
3. Swagger UI otomatik açılacak

## 🧪 Test Etme

### Postman ile Test

1. **Health Check**
   - Method: GET
   - URL: `http://localhost:5000/health`

2. **Kategorileri Getir**
   - Method: GET
   - URL: `http://localhost:5000/api/categories`

3. **Ürün Ekle**
   - Method: POST
   - URL: `http://localhost:5000/api/products`
   - Headers: `Content-Type: application/json`
   - Body: (yukarıdaki JSON örneği)

### cURL ile Test

```bash
# Health check
curl http://localhost:5000/health

# Kategorileri listele
curl http://localhost:5000/api/categories

# Ürün ekle
curl -X POST http://localhost:5000/api/products \
  -H "Content-Type: application/json" \
  -d '{
    "code": "TEST-001",
    "name": "Test Ürünü",
    "categoryCode": "T48",
    "price": 299.99,
    "stock": 100
  }'
```

## 📁 Proje Yapısı

```
TSoftApiClient/
├── Controllers/           # API endpoint'leri
│   ├── ProductsController.cs
│   └── CategoriesController.cs
├── Services/             # İş mantığı servisleri
│   └── TSoftApiService.cs
├── Models/               # Veri modelleri
│   └── TSoftApiResponse.cs
├── DTOs/                 # Data Transfer Objects
│   └── ProductDto.cs
├── Program.cs           # Uygulama başlangıcı
└── appsettings.json     # Yapılandırma
```

## ⚙️ Yapılandırma

### appsettings.json
```json
{
  "TSoftApi": {
    "Token": "your-token-here",
    "BaseUrl": "https://wawtesettur.tsoft.biz/rest1"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
```

### Environment Variables (Alternatif)
```bash
# Linux/Mac
export TSoftApi__Token="your-token-here"
export TSoftApi__BaseUrl="https://wawtesettur.tsoft.biz/rest1"

# Windows
set TSoftApi__Token=your-token-here
set TSoftApi__BaseUrl=https://wawtesettur.tsoft.biz/rest1
```

## 🐛 Sorun Giderme

### Port Çakışması
Eğer 5000 portu kullanılıyorsa, `Properties/launchSettings.json` dosyasını düzenle:
```json
"applicationUrl": "http://localhost:5001"
```

### SSL Hatası
Geliştirme sertifikasını güvenilir yap:
```bash
dotnet dev-certs https --trust
```

## 📖 Daha Fazla Bilgi

- [ASP.NET Core Dokümantasyonu](https://docs.microsoft.com/aspnet/core)
- [Swagger/OpenAPI](https://swagger.io/)
- [.NET 9 Yenilikleri](https://devblogs.microsoft.com/dotnet/announcing-dotnet-9/)

## 🤝 Katkıda Bulunma

Pull request'ler memnuniyetle karşılanır!

## 📄 Lisans

MIT License


ssss