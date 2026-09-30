using ShopNet.Domain.Common;

namespace ShopNet.Domain.Entities;

public class Cart : AuditableEntity
{
    public int UserId { get; set; }
    public User? User { get; set; }

    public ICollection<CartItem> Items { get; set; } = new List<CartItem>();
}
