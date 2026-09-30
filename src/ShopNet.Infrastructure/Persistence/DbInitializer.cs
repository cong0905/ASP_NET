using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ShopNet.Application.Common.Interfaces;
using ShopNet.Domain.Entities;
using ShopNet.Domain.Enums;

namespace ShopNet.Infrastructure.Persistence;

public class DbInitializer
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<DbInitializer> _logger;

    public DbInitializer(
        ApplicationDbContext context,
        IPasswordHasher passwordHasher,
        ILogger<DbInitializer> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        try
        {
            await _context.Database.EnsureCreatedAsync();
            await SeedDataAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Đã xảy ra lỗi trong quá trình khởi tạo dữ liệu mẫu.");
            throw;
        }
    }

    public async Task SeedDataAsync()
    {
        // 1. Seed Users
        if (!await _context.Users.AnyAsync())
        {
            var adminUser = new User
            {
                Username = "admin",
                Email = "admin@shopnet.com",
                FullName = "System Administrator",
                PasswordHash = _passwordHasher.Hash("Admin@123"),
                Role = UserRole.Admin,
                CreatedAt = DateTime.UtcNow
            };

            var customerUser = new User
            {
                Username = "customer",
                Email = "customer@shopnet.com",
                FullName = "Nguyễn Văn Khách",
                PasswordHash = _passwordHasher.Hash("Customer@123"),
                Role = UserRole.Customer,
                CreatedAt = DateTime.UtcNow
            };
            customerUser.Cart = new Cart { User = customerUser };

            _context.Users.AddRange(adminUser, customerUser);
            await _context.SaveChangesAsync();
        }

        // 2. Seed Categories
        if (!await _context.Categories.AnyAsync())
        {
            var categories = new List<Category>
            {
                new() { Name = "Laptops & Máy Tính", Slug = "laptops-computers", Description = "Máy tính xách tay cao cấp, Ultrabook và PC Gaming" },
                new() { Name = "Điện Thoại & Máy Tính Bảng", Slug = "smartphones-tablets", Description = "Các dòng Smartphone cao cấp, Flagship mới nhất" },
                new() { Name = "Âm Thanh & Tai Nghe", Slug = "audio-accessories", Description = "Tai nghe True Wireless, chống ồn chủ động và Loa Bluetooth" },
                new() { Name = "Phụ Kiện Gaming & Văn Phòng", Slug = "gaming-gear", Description = "Bàn phím cơ, chuột công thái học, màn hình đồ họa" }
            };

            _context.Categories.AddRange(categories);
            await _context.SaveChangesAsync();
        }

        // 3. Seed Products
        if (!await _context.Products.AnyAsync())
        {
            var catLaptops = await _context.Categories.FirstAsync(c => c.Slug == "laptops-computers");
            var catPhones = await _context.Categories.FirstAsync(c => c.Slug == "smartphones-tablets");
            var catAudio = await _context.Categories.FirstAsync(c => c.Slug == "audio-accessories");
            var catGear = await _context.Categories.FirstAsync(c => c.Slug == "gaming-gear");

            var products = new List<Product>
            {
                new()
                {
                    Name = "MacBook Pro 14 M3 Pro (18GB / 512GB)",
                    SKU = "APL-MBP14-M3P",
                    Description = "Chip Apple M3 Pro siêu mạnh mẽ, màn hình Liquid Retina XDR 120Hz rực rỡ, pin lên tới 18 tiếng.",
                    Price = 1999.00m,
                    StockQuantity = 25,
                    CategoryId = catLaptops.Id,
                    ImageUrl = "https://images.unsplash.com/photo-1517336714731-489689fd1ca8?auto=format&fit=crop&w=800&q=80",
                    IsActive = true
                },
                new()
                {
                    Name = "Dell XPS 15 9530 (i7-13700H / RTX 4060 / 32GB)",
                    SKU = "DEL-XPS15-9530",
                    Description = "Thiết kế viền siêu mỏng InfinityEdge OLED 3.5K, hiệu năng đồ họa ấn tượng cho sáng tạo nội dung.",
                    Price = 1799.00m,
                    StockQuantity = 15,
                    CategoryId = catLaptops.Id,
                    ImageUrl = "https://images.unsplash.com/photo-1593642632823-8f785ba67e45?auto=format&fit=crop&w=800&q=80",
                    IsActive = true
                },
                new()
                {
                    Name = "iPhone 15 Pro Max 256GB Titan Tự Nhiên",
                    SKU = "APL-IP15PM-256",
                    Description = "Khung titan siêu nhẹ siêu bền, chip Apple A17 Pro 3nm đỉnh cao, camera tiềm vọng zoom quang 5x.",
                    Price = 1199.00m,
                    StockQuantity = 50,
                    CategoryId = catPhones.Id,
                    ImageUrl = "https://images.unsplash.com/photo-1695048133142-1a20484d2569?auto=format&fit=crop&w=800&q=80",
                    IsActive = true
                },
                new()
                {
                    Name = "Samsung Galaxy S24 Ultra 512GB Titanium Gray",
                    SKU = "SAM-S24U-512",
                    Description = "Quyền năng Galaxy AI dẫn đầu xu hướng, bút S-Pen tích hợp, màn hình phẳng chống chói xuất sắc.",
                    Price = 1299.00m,
                    StockQuantity = 35,
                    CategoryId = catPhones.Id,
                    ImageUrl = "https://images.unsplash.com/photo-1610945415295-d9bbf067e59c?auto=format&fit=crop&w=800&q=80",
                    IsActive = true
                },
                new()
                {
                    Name = "Sony WH-1000XM5 Chống Ồn Cao Cấp",
                    SKU = "SNY-WH1000XM5",
                    Description = "Công nghệ chống ồn hàng đầu ngành, 8 micro thu âm chuẩn phòng thu, chất âm Hi-Res LDAC chi tiết.",
                    Price = 349.00m,
                    StockQuantity = 40,
                    CategoryId = catAudio.Id,
                    ImageUrl = "https://images.unsplash.com/photo-1546435770-a3e426bf472b?auto=format&fit=crop&w=800&q=80",
                    IsActive = true
                },
                new()
                {
                    Name = "AirPods Pro 2 MagSafe (USB-C)",
                    SKU = "APL-APP2-USBC",
                    Description = "Chip H2 nâng cấp khả năng khử tiếng ồn gấp 2 lần, Adaptive Audio thông minh và chống bụi nước IP54.",
                    Price = 249.00m,
                    StockQuantity = 60,
                    CategoryId = catAudio.Id,
                    ImageUrl = "https://images.unsplash.com/photo-1600294037681-c80b4cb5b434?auto=format&fit=crop&w=800&q=80",
                    IsActive = true
                },
                new()
                {
                    Name = "Chuột Công Thái Học Logitech MX Master 3S",
                    SKU = "LOG-MXM3S-GR",
                    Description = "Cảm biến 8K DPI Darkfield lướt trên mọi bề mặt, nút cuộn điện từ MagSpeed êm ái Quiet Clicks.",
                    Price = 99.00m,
                    StockQuantity = 80,
                    CategoryId = catGear.Id,
                    ImageUrl = "https://images.unsplash.com/photo-1615663245857-ac93bb7c39e7?auto=format&fit=crop&w=800&q=80",
                    IsActive = true
                },
                new()
                {
                    Name = "Bàn Phím Cơ Custom Keychron Q1 Pro Wireless",
                    SKU = "KEY-Q1PRO-RD",
                    Description = "Vỏ nhôm CNC nguyên khối, gasket mount êm ái, switch Gateron Jupiter Red mượt mà, hỗ trợ QMK/VIA.",
                    Price = 199.00m,
                    StockQuantity = 30,
                    CategoryId = catGear.Id,
                    ImageUrl = "https://images.unsplash.com/photo-1587829741301-dc798b83add3?auto=format&fit=crop&w=800&q=80",
                    IsActive = true
                }
            };

            _context.Products.AddRange(products);
            await _context.SaveChangesAsync();
        }
    }
}
