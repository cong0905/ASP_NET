using Microsoft.Extensions.Configuration;
using Moq;
using ShopNet.Application.Common.Interfaces;
using ShopNet.Infrastructure.Services.Payment;
using Xunit;

namespace ShopNet.UnitTests.Services;

public class PaymentServiceTests
{
    private readonly Mock<IConfiguration> _configMock;

    public PaymentServiceTests()
    {
        _configMock = new Mock<IConfiguration>();
        _configMock.Setup(c => c["VnPay:BaseUrl"]).Returns("https://sandbox.vnpayment.vn/paymentv2/vpcpay.html");
        _configMock.Setup(c => c["VnPay:TmnCode"]).Returns("TEST_TMN");
        _configMock.Setup(c => c["VnPay:HashSecret"]).Returns("TEST_SECRET_KEY");
        _configMock.Setup(c => c["VnPay:ReturnUrl"]).Returns("http://localhost:3000/checkout/vnpay-return");
    }

    [Fact]
    public void VnPayService_CreatePaymentUrl_ShouldContainEssentialParametersAndHash()
    {
        // Arrange
        var service = new VnPayService(_configMock.Object);
        var request = new VnPayRequest
        {
            OrderNumber = "ORD-TEST-001",
            Amount = 150000,
            OrderInfo = "Test Payment Order 001",
            IpAddress = "127.0.0.1"
        };

        // Act
        var url = service.CreatePaymentUrl(request);

        // Assert
        Assert.NotNull(url);
        Assert.StartsWith("https://sandbox.vnpayment.vn/paymentv2/vpcpay.html?", url);
        Assert.Contains("vnp_TmnCode=TEST_TMN", url);
        Assert.Contains("vnp_Amount=15000000", url); // VNPay nhân 100
        Assert.Contains("vnp_TxnRef=ORD-TEST-001", url);
        Assert.Contains("vnp_SecureHash=", url);
    }

    [Fact]
    public void VnPayService_ProcessReturn_WithValidHash_ShouldReturnSuccess()
    {
        // Arrange
        var service = new VnPayService(_configMock.Object);
        var secret = "TEST_SECRET_KEY";

        var rawSignData = "vnp_Amount=15000000&vnp_OrderInfo=Test+Payment&vnp_ResponseCode=00&vnp_TmnCode=TEST_TMN&vnp_TransactionNo=14123456&vnp_TxnRef=ORD-TEST-001";
        var hash = VnPayService.HmacSha512(secret, rawSignData);

        var queryParams = new Dictionary<string, string>
        {
            { "vnp_Amount", "15000000" },
            { "vnp_OrderInfo", "Test Payment" },
            { "vnp_ResponseCode", "00" },
            { "vnp_TmnCode", "TEST_TMN" },
            { "vnp_TransactionNo", "14123456" },
            { "vnp_TxnRef", "ORD-TEST-001" },
            { "vnp_SecureHash", hash }
        };

        // Act
        var result = service.ProcessReturn(queryParams);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("ORD-TEST-001", result.OrderNumber);
        Assert.Equal(150000m, result.Amount);
    }
}
