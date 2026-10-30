namespace ShopNet.Application.Common.Interfaces;

public class MoMoPaymentRequest
{
    public string OrderId { get; set; } = string.Empty;
    public string OrderInfo { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string ReturnUrl { get; set; } = string.Empty;
    public string NotifyUrl { get; set; } = string.Empty;
    public string ExtraData { get; set; } = string.Empty;
}

public class MoMoPaymentResult
{
    public bool IsSuccess { get; set; }
    public string PayUrl { get; set; } = string.Empty;
    public string QrCodeUrl { get; set; } = string.Empty;
    public string Deeplink { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int ResultCode { get; set; }
}

public class MoMoIpnRequest
{
    public string PartnerCode { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
    public string RequestId { get; set; } = string.Empty;
    public long Amount { get; set; }
    public string OrderInfo { get; set; } = string.Empty;
    public string OrderType { get; set; } = string.Empty;
    public long TransId { get; set; }
    public int ResultCode { get; set; }
    public string Message { get; set; } = string.Empty;
    public string PayType { get; set; } = string.Empty;
    public long ResponseTime { get; set; }
    public string ExtraData { get; set; } = string.Empty;
    public string Signature { get; set; } = string.Empty;
}

public interface IMoMoService
{
    Task<MoMoPaymentResult> CreatePaymentAsync(MoMoPaymentRequest request, CancellationToken cancellationToken = default);
    bool ValidateSignature(MoMoIpnRequest request);
}
