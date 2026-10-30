using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using ShopNet.Application.Common.Interfaces;

namespace ShopNet.Infrastructure.Services.Payment;

public class MoMoService : IMoMoService
{
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;

    public MoMoService(IConfiguration configuration, HttpClient httpClient)
    {
        _configuration = configuration;
        _httpClient = httpClient;
    }

    public async Task<MoMoPaymentResult> CreatePaymentAsync(MoMoPaymentRequest request, CancellationToken cancellationToken = default)
    {
        var endpoint = _configuration["MoMo:Endpoint"] ?? "https://test-payment.momo.vn/v2/gateway/api/create";
        var partnerCode = _configuration["MoMo:PartnerCode"] ?? "MOMO_DEMO";
        var accessKey = _configuration["MoMo:AccessKey"] ?? "MOMO_ACCESS_KEY";
        var secretKey = _configuration["MoMo:SecretKey"] ?? "MOMO_SECRET_KEY";
        var returnUrl = string.IsNullOrWhiteSpace(request.ReturnUrl)
            ? (_configuration["MoMo:ReturnUrl"] ?? "http://localhost:3000/checkout/momo-return")
            : request.ReturnUrl;
        var notifyUrl = string.IsNullOrWhiteSpace(request.NotifyUrl)
            ? (_configuration["MoMo:NotifyUrl"] ?? "https://example.com/api/payments/momo-ipn")
            : request.NotifyUrl;

        var requestId = Guid.NewGuid().ToString();
        var rawAmount = ((long)request.Amount).ToString();
        var requestType = "captureWallet";
        var extraData = request.ExtraData ?? "";

        // Format signature theo MoMo API v2:
        // accessKey=$accessKey&amount=$amount&extraData=$extraData&ipnUrl=$ipnUrl&orderId=$orderId&orderInfo=$orderInfo&partnerCode=$partnerCode&redirectUrl=$redirectUrl&requestId=$requestId&requestType=$requestType
        var rawHash = $"accessKey={accessKey}&amount={rawAmount}&extraData={extraData}&ipnUrl={notifyUrl}&orderId={request.OrderId}&orderInfo={request.OrderInfo}&partnerCode={partnerCode}&redirectUrl={returnUrl}&requestId={requestId}&requestType={requestType}";
        var signature = HmacSha256(secretKey, rawHash);

        var requestBody = new
        {
            partnerCode,
            partnerName = "ShopNet E-Commerce",
            storeId = "ShopNetStore",
            requestId,
            amount = (long)request.Amount,
            orderId = request.OrderId,
            orderInfo = request.OrderInfo,
            redirectUrl = returnUrl,
            ipnUrl = notifyUrl,
            lang = "vi",
            extraData,
            requestType,
            signature
        };

        var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        try
        {
            var response = await _httpClient.PostAsync(endpoint, jsonContent, cancellationToken);
            var responseString = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(responseString);
            var root = doc.RootElement;

            int resultCode = root.TryGetProperty("resultCode", out var rc) ? rc.GetInt32() : -1;
            string message = root.TryGetProperty("message", out var msg) ? msg.GetString() ?? "" : "";
            string payUrl = root.TryGetProperty("payUrl", out var pu) ? pu.GetString() ?? "" : "";
            string qrCodeUrl = root.TryGetProperty("qrCodeUrl", out var qr) ? qr.GetString() ?? "" : "";
            string deeplink = root.TryGetProperty("deeplink", out var dl) ? dl.GetString() ?? "" : "";

            return new MoMoPaymentResult
            {
                IsSuccess = resultCode == 0,
                ResultCode = resultCode,
                Message = message,
                PayUrl = payUrl,
                QrCodeUrl = qrCodeUrl,
                Deeplink = deeplink
            };
        }
        catch (Exception ex)
        {
            return new MoMoPaymentResult
            {
                IsSuccess = false,
                ResultCode = -1,
                Message = $"Không thể kết nối cổng MoMo: {ex.Message}"
            };
        }
    }

    public bool ValidateSignature(MoMoIpnRequest request)
    {
        var accessKey = _configuration["MoMo:AccessKey"] ?? "MOMO_ACCESS_KEY";
        var secretKey = _configuration["MoMo:SecretKey"] ?? "MOMO_SECRET_KEY";

        // Signature IPN MoMo:
        // accessKey=$accessKey&amount=$amount&extraData=$extraData&message=$message&orderId=$orderId&orderInfo=$orderInfo&orderType=$orderType&partnerCode=$partnerCode&payType=$payType&requestId=$requestId&responseTime=$responseTime&resultCode=$resultCode&transId=$transId
        var rawHash = $"accessKey={accessKey}&amount={request.Amount}&extraData={request.ExtraData}&message={request.Message}&orderId={request.OrderId}&orderInfo={request.OrderInfo}&orderType={request.OrderType}&partnerCode={request.PartnerCode}&payType={request.PayType}&requestId={request.RequestId}&responseTime={request.ResponseTime}&resultCode={request.ResultCode}&transId={request.TransId}";
        var computedSignature = HmacSha256(secretKey, rawHash);

        return string.Equals(computedSignature, request.Signature, StringComparison.OrdinalIgnoreCase);
    }

    public static string HmacSha256(string key, string inputData)
    {
        byte[] keyBytes = Encoding.UTF8.GetBytes(key);
        byte[] inputBytes = Encoding.UTF8.GetBytes(inputData);
        using (var hmac = new HMACSHA256(keyBytes))
        {
            byte[] hashValue = hmac.ComputeHash(inputBytes);
            return Convert.ToHexString(hashValue).ToLowerInvariant();
        }
    }
}
