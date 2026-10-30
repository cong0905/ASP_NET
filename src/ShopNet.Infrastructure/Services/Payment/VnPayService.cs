using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using ShopNet.Application.Common.Interfaces;

namespace ShopNet.Infrastructure.Services.Payment;

public class VnPayService : IVnPayService
{
    private readonly IConfiguration _configuration;

    public VnPayService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string CreatePaymentUrl(VnPayRequest request)
    {
        var vnpUrl = _configuration["VnPay:BaseUrl"] ?? "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
        var vnpTmnCode = _configuration["VnPay:TmnCode"] ?? "DEMO_TMN_CODE";
        var vnpHashSecret = _configuration["VnPay:HashSecret"] ?? "DEMO_HASH_SECRET_KEY_1234567890";
        var vnpReturnUrl = _configuration["VnPay:ReturnUrl"] ?? "http://localhost:3000/checkout/vnpay-return";

        var vnpData = new SortedList<string, string>(new VnPayCompare())
        {
            { "vnp_Version", "2.1.0" },
            { "vnp_Command", "pay" },
            { "vnp_TmnCode", vnpTmnCode },
            { "vnp_Amount", ((long)(request.Amount * 100)).ToString(CultureInfo.InvariantCulture) },
            { "vnp_CreateDate", DateTime.UtcNow.ToString("yyyyMMddHHmmss") },
            { "vnp_CurrCode", "VND" },
            { "vnp_IpAddr", string.IsNullOrWhiteSpace(request.IpAddress) ? "127.0.0.1" : request.IpAddress },
            { "vnp_Locale", "vn" },
            { "vnp_OrderInfo", request.OrderInfo },
            { "vnp_OrderType", "other" },
            { "vnp_ReturnUrl", vnpReturnUrl },
            { "vnp_TxnRef", request.OrderNumber }
        };

        var queryBuilder = new StringBuilder();
        var signDataBuilder = new StringBuilder();

        foreach (var (key, value) in vnpData)
        {
            if (!string.IsNullOrEmpty(value))
            {
                queryBuilder.Append(WebUtility.UrlEncode(key) + "=" + WebUtility.UrlEncode(value) + "&");
                signDataBuilder.Append(WebUtility.UrlEncode(key) + "=" + WebUtility.UrlEncode(value) + "&");
            }
        }

        var queryString = queryBuilder.ToString().TrimEnd('&');
        var signData = signDataBuilder.ToString().TrimEnd('&');
        var secureHash = HmacSha512(vnpHashSecret, signData);

        return $"{vnpUrl}?{queryString}&vnp_SecureHash={secureHash}";
    }

    public VnPayResponse ProcessReturn(IDictionary<string, string> queryParams)
    {
        return ParseVnPayResponse(queryParams);
    }

    public VnPayResponse ProcessIpn(IDictionary<string, string> queryParams)
    {
        return ParseVnPayResponse(queryParams);
    }

    private VnPayResponse ParseVnPayResponse(IDictionary<string, string> queryParams)
    {
        var vnpHashSecret = _configuration["VnPay:HashSecret"] ?? "DEMO_HASH_SECRET_KEY_1234567890";
        var sortedParams = new SortedList<string, string>(new VnPayCompare());
        string receivedSecureHash = string.Empty;

        foreach (var (k, v) in queryParams)
        {
            if (string.Equals(k, "vnp_SecureHash", StringComparison.OrdinalIgnoreCase))
            {
                receivedSecureHash = v;
            }
            else if (k.StartsWith("vnp_", StringComparison.OrdinalIgnoreCase))
            {
                sortedParams[k] = v;
            }
        }

        var signDataBuilder = new StringBuilder();
        foreach (var (key, value) in sortedParams)
        {
            if (!string.IsNullOrEmpty(value))
            {
                signDataBuilder.Append(WebUtility.UrlEncode(key) + "=" + WebUtility.UrlEncode(value) + "&");
            }
        }

        var signData = signDataBuilder.ToString().TrimEnd('&');
        var calculatedHash = HmacSha512(vnpHashSecret, signData);

        bool isSignatureValid = string.Equals(receivedSecureHash, calculatedHash, StringComparison.OrdinalIgnoreCase);

        var responseCode = queryParams.TryGetValue("vnp_ResponseCode", out var code) ? code : "99";
        var orderNumber = queryParams.TryGetValue("vnp_TxnRef", out var txn) ? txn : string.Empty;
        var transNo = queryParams.TryGetValue("vnp_TransactionNo", out var tn) ? tn : string.Empty;
        decimal amount = 0;
        if (queryParams.TryGetValue("vnp_Amount", out var amtStr) && long.TryParse(amtStr, out var rawAmt))
        {
            amount = rawAmt / 100m;
        }

        bool isSuccess = isSignatureValid && responseCode == "00";

        return new VnPayResponse
        {
            IsSuccess = isSuccess,
            OrderNumber = orderNumber,
            TransactionNo = transNo,
            ResponseCode = responseCode,
            Message = isSuccess ? "Giao dịch thành công" : (isSignatureValid ? $"Giao dịch thất bại (mã {responseCode})" : "Chữ ký không hợp lệ"),
            Amount = amount,
            RawData = queryParams
        };
    }

    public static string HmacSha512(string key, string inputData)
    {
        var hash = new StringBuilder();
        byte[] keyBytes = Encoding.UTF8.GetBytes(key);
        byte[] inputBytes = Encoding.UTF8.GetBytes(inputData);
        using (var hmac = new HMACSHA512(keyBytes))
        {
            byte[] hashValue = hmac.ComputeHash(inputBytes);
            foreach (var b in hashValue)
            {
                hash.Append(b.ToString("x2"));
            }
        }
        return hash.ToString();
    }
}

internal class VnPayCompare : IComparer<string>
{
    public int Compare(string? x, string? y)
    {
        if (x == y) return 0;
        if (x == null) return -1;
        if (y == null) return 1;
        return CompareInfo.GetCompareInfo("en-US").Compare(x, y, CompareOptions.Ordinal);
    }
}
