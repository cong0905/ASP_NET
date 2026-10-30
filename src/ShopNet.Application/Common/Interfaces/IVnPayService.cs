namespace ShopNet.Application.Common.Interfaces;

public class VnPayRequest
{
    public string OrderNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string OrderInfo { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
}

public class VnPayResponse
{
    public bool IsSuccess { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string TransactionNo { get; set; } = string.Empty;
    public string ResponseCode { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public IDictionary<string, string> RawData { get; set; } = new Dictionary<string, string>();
}

public interface IVnPayService
{
    string CreatePaymentUrl(VnPayRequest request);
    VnPayResponse ProcessReturn(IDictionary<string, string> queryParams);
    VnPayResponse ProcessIpn(IDictionary<string, string> queryParams);
}
