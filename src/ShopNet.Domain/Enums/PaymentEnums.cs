namespace ShopNet.Domain.Enums;

public enum PaymentMethod
{
    COD = 1,
    CreditCard = 2,
    VNPay = 3,
    MoMo = 4,
    BankTransfer = 5
}

public enum PaymentStatus
{
    Pending = 1,
    Completed = 2,
    Failed = 3,
    Refunded = 4
}
