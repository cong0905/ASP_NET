using ShopNet.Domain.Common;
using ShopNet.Domain.Enums;

namespace ShopNet.Domain.Entities;

public class PaymentTransaction : AuditableEntity
{
    public int OrderId { get; set; }
    public Order? Order { get; set; }

    public string TransactionId { get; set; } = string.Empty;
    public PaymentMethod Provider { get; set; }
    public decimal Amount { get; set; }
    public string? OrderDescription { get; set; }

    public string? GatewayTransactionNo { get; set; }
    public string? ResponseCode { get; set; }
    public string? Message { get; set; }
    public bool IsSuccess { get; set; }
    public string? RawResponse { get; set; }
}
