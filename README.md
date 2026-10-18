# ShopNet · E-Commerce API

Backend thương mại điện tử với **ASP.NET Core 8**, **Entity Framework Core / SQLite**, **CQRS và MediatR**. Dự án minh họa cách tổ chức API theo Clean Architecture, từ xác thực và danh mục đến giỏ hàng, đơn hàng và dashboard quản trị.

[Chạy nhanh](#chạy-nhanh) · [API](#các-nhóm-api) · [Docker](#chạy-với-docker) · [Kiểm thử](#kiểm-thử)

## Chức năng

- Đăng ký, đăng nhập, JWT access/refresh token, hồ sơ và đổi mật khẩu.
- Sản phẩm: tìm kiếm, lọc, phân trang, quản trị và đánh giá.
- Danh mục và giỏ hàng theo tài khoản.
- Checkout, lịch sử/chi tiết đơn hàng, cập nhật trạng thái và hủy đơn.
- Endpoint thanh toán **mô phỏng**, dashboard thống kê cho Admin.
- Validation pipeline, xử lý lỗi tập trung và ghi thông tin audit khi lưu dữ liệu.

Đây là dự án thực hành backend. Thanh toán mô phỏng không thực hiện giao dịch tiền thật; repository không tự chứng minh mức độ sẵn sàng vận hành production.

## Kiến trúc

| Project | Trách nhiệm |
| --- | --- |
| `ShopNet.Domain` | Entity, enum và ngoại lệ nghiệp vụ |
| `ShopNet.Application` | Command/query, handler, validator và interface |
| `ShopNet.Infrastructure` | EF Core, SQLite, JWT, password hashing, dịch vụ hạ tầng |
| `ShopNet.API` | Controller, middleware, Swagger và cấu hình ứng dụng |
| `ShopNet.UnitTests` | Kiểm thử với xUnit và các công cụ hỗ trợ |

Luồng request: controller → MediatR → handler → interface dữ liệu/dịch vụ → implementation hạ tầng. Các tầng nằm trong `src/`; kiểm thử nằm trong `tests/`.

## Chạy nhanh

Cần **.NET 8 SDK** và Git. Bản hiện tại dùng SQLite nên không cần cài SQL Server.

```powershell
git clone https://github.com/cong0905/ASP_NET.git
cd ASP_NET
dotnet restore ShopNet.sln
dotnet build ShopNet.sln
dotnet run --project src/ShopNet.API/ShopNet.API.csproj --launch-profile http
```

Mở **Swagger UI tại `http://localhost:5156/`**. Đường dẫn JSON là `/swagger/v1/swagger.json`; Swagger UI được cấu hình ở gốc ứng dụng. Port `5156` lấy từ launch profile `http`; xem URL terminal nếu chạy bằng cấu hình khác.

Startup gọi `DbInitializer` để khởi tạo dữ liệu. Nếu khởi tạo lỗi, xem log trước khi thử API.

### Tài khoản seed dành cho local

| Vai trò | Email | Mật khẩu mẫu |
| --- | --- | --- |
| Admin | `admin@shopnet.com` | `Admin@123` |
| Customer | `customer@shopnet.com` | `Customer@123` |

Đăng nhập ở `/api/auth/login` với `emailOrUsername` và `password`; dùng token trả về trong nút **Authorize** theo dạng `Bearer <token>`. Đây là dữ liệu mẫu từ seeder, không dùng cho triển khai thật.

## Các nhóm API

| Nhóm | Prefix | Ví dụ |
| --- | --- | --- |
| Xác thực | `/api/auth` | `POST /login`, `POST /register`, `GET /me` |
| Sản phẩm | `/api/products` | `GET /`, `GET /{id}`, tạo/sửa/xóa và review |
| Danh mục | `/api/categories` | `GET /`, `POST /` |
| Giỏ hàng | `/api/cart` | `GET /`, `POST /items`, `PUT /items`, `DELETE /items/{productId}` |
| Đơn hàng | `/api/orders` | `POST /checkout`, `GET /`, `PATCH /{id}/status`, hủy và thanh toán mô phỏng |
| Quản trị | `/api/admin` | `GET /dashboard` |

Các đường dẫn ví dụ nối với prefix cùng hàng. Xem Swagger và [controller](src/ShopNet.API/Controllers) để biết schema, quyền truy cập và response cụ thể.

## Cấu hình

| Khóa cấu hình | Ý nghĩa |
| --- | --- |
| `ConnectionStrings:DefaultConnection` | Kết nối SQLite, mặc định fallback `Data Source=shopnet.db` |
| `Jwt:Key`, `Jwt:Issuer`, `Jwt:Audience` | Khóa ký và thông tin xác thực JWT |

Có thể ghi đè bằng biến môi trường .NET, ví dụ `Jwt__Key` và `ConnectionStrings__DefaultConnection`. Cấu hình hiện tại phục vụ development; cần thay khóa mặc định, giới hạn CORS và rà soát HTTPS trước khi triển khai.

## Chạy với Docker

```powershell
docker compose up -d --build
docker compose logs -f shopnet-api
```

Mở `http://localhost:5000/`. Compose ánh xạ cổng host `5000` sang container `8080` và lưu SQLite trong volume `shopnet-data`.

```powershell
docker compose down
```

## Kiểm thử

```powershell
dotnet test ShopNet.sln
```

Repository có [workflow CI](.github/workflows/ci.yml) và [bộ test](tests/ShopNet.UnitTests). Hãy xem kết quả của từng lần chạy; README không thay thế bằng chứng test hoặc đánh giá bảo mật.

## Giấy phép

[MIT](LICENSE). **Tác giả:** [Nguyễn Đức Công](https://github.com/cong0905).
