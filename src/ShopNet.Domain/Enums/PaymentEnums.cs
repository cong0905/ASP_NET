namespace ShopNet.Domain.Enums;

public enum PaymentMethod
{
    COD = 1,
    CreditCard = 2,
    VNPay_Mock = 3
}

public enum PaymentStatus
{
    Pending = 1,
    Completed = 2,
    Failed = 3
}
