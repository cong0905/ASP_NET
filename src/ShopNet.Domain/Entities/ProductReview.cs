using ShopNet.Domain.Common;

namespace ShopNet.Domain.Entities;

public class ProductReview : AuditableEntity
{
    public int ProductId { get; set; }
    public Product? Product { get; set; }

    public int UserId { get; set; }
    public User? User { get; set; }

    public int Rating { get; set; } // 1 - 5 stars
    public string Comment { get; set; } = string.Empty;
}
