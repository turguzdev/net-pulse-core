# ⚡ NetPulse Core - High-Performance .NET 8 Microservice & Telemetry Engine

<div align="center">

![.NET 8](https://img.shields.io/badge/.NET_8.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![C# 12](https://img.shields.io/badge/C%23_12-239120?style=for-the-badge&logo=csharp&logoColor=white)
![ASP.NET Core](https://img.shields.io/badge/ASP.NET_Core_Minimal_APIs-0078D7?style=for-the-badge&logo=microsoft&logoColor=white)
![EF Core SQLite](https://img.shields.io/badge/EF_Core_SQLite-003B57?style=for-the-badge&logo=sqlite&logoColor=white)
![Swagger OpenAPI](https://img.shields.io/badge/OpenAPI_Swagger_v1-85EA2D?style=for-the-badge&logo=swagger&logoColor=black)
![License](https://img.shields.io/badge/License-MIT-emerald?style=for-the-badge)

**NetPulse Core**, modern kurumsal mimariler için tasarlanmış yüksek verimlilikli, asenkron, `System.Threading.Channels` tabanlı iş kuyruğu motoru ve gerçek zamanlı telemetri & küme yönetim platformudur.

</div>

---

## 🌟 Neden Farklı? (Mimari ve Teknoloji)

Bu proje, JavaScript/TypeScript ve Python ekosistemlerinden tamamen bağımsız olarak **.NET 8 (C# 12)** platformunda geliştirilmiştir:

- **Ultra Hızlı Minimal APIs**: ASP.NET Core 8 minimal API mimarisiyle sıfır ek yük, mikro saniye düzeyinde gecikme.
- **Asenkron Kanal İşleme (`Channel<T>`)**: Lock-free, bounded multi-producer single-consumer arka plan iş yürütme motoru (`JobProcessorWorker`).
- **Canlı SSE Telemetri Akışı**: Server-Sent Events ile CPU yükü, GC bellek yönetimi (Gen 0/1/2), ThreadPool istatistikleri ve küme düğüm olayları anlık olarak istemcilere iletilir.
- **Entity Framework Core & SQLite**: Otomatik şema oluşturma ve tohumlama ile bağımsız, taşınabilir veri katmanı.
- **Dahili İnteraktif Gösterge Paneli**: Web tarayıcısında çalışan modern koyu mod kontrol merkezi (`/`) ve Swagger arayüzü (`/swagger`).

---

## 🏛️ Mimari Şema

```
[İstemci / Dashboard / API Consumers]
               │
               ▼
   [ASP.NET Core 8 Minimal API]
   ├── /api/v1/overview
   ├── /api/v1/telemetry/live
   ├── /api/v1/telemetry/stream (SSE)
   ├── /api/v1/nodes (Cluster Mesh)
   └── /api/v1/jobs  (Task Pipeline)
               │
      ┌────────┴────────────────────┐
      ▼                             ▼
[Channel<JobTask>]          [AppDbContext (SQLite)]
      │                             │
      ▼                             ▼
[JobProcessorWorker] ──► [EventBroadcaster] ──► [SSE Clients]
```

---

## 🚀 Hızlı Başlangıç

### Gereksinimler
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### 1. Çalıştırma

Klasör içindeki `BASLAT.bat` dosyasına çift tıklayabilir veya terminalde şu komutu çalıştırabilirsiniz:

```bash
# Projeyi derle ve başlat
dotnet run -c Release --urls "http://localhost:5000"
```

### 2. Canlı Erişim Noktaları
- 📊 **Web Kontrol Paneli**: [http://localhost:5000](http://localhost:5000)
- 📖 **Swagger / OpenAPI**: [http://localhost:5000/swagger](http://localhost:5000/swagger)
- 💓 **Sağlık Durumu**: [http://localhost:5000/api/v1/health](http://localhost:5000/api/v1/health)
- 📡 **Canlı Telemetri**: [http://localhost:5000/api/v1/telemetry/live](http://localhost:5000/api/v1/telemetry/live)

---

## 📦 REST API Uç Noktaları

| Metod | Uç Nokta | Açıklama |
|---|---|---|
| `GET` | `/api/v1/overview` | Küme genel durum özeti ve sistem yükü |
| `GET` | `/api/v1/telemetry/live` | Anlık donanım & CLR runtime telemetrisi |
| `GET` | `/api/v1/telemetry/stream` | Gerçek zamanlı Server-Sent Events akışı |
| `GET` | `/api/v1/health` | Veritabanı ve servis sağlık kontrolü |
| `GET` | `/api/v1/nodes` | Kayıtlı küme düğümlerini listele |
| `POST` | `/api/v1/nodes` | Kümeye yeni düğüm kaydet |
| `POST` | `/api/v1/nodes/{id}/heartbeat` | Düğüm heartbeat sinyali gönder |
| `DELETE` | `/api/v1/nodes/{id}` | Düğümü kümeden çıkar |
| `GET` | `/api/v1/jobs` | Arka plan iş kuyruğunu listele |
| `POST` | `/api/v1/jobs` | Kanala yeni asenkron görev ekle |
| `GET` | `/api/v1/jobs/{id}` | Görev ilerleme durumunu ve sonucunu sorgula |

---

## 🐳 Docker ile Çalıştırma

```bash
# Docker imajı oluştur
docker build -t net-pulse-core .

# Konteyneri başlat
docker run -d -p 5000:8080 --name netpulse net-pulse-core
```

---

## 📄 Lisans
Bu proje [MIT](LICENSE) lisansı altında geliştirilmiştir.
