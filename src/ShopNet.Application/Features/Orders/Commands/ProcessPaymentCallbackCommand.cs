using MediatR;
using Microsoft.EntityFrameworkCore;
using ShopNet.Application.Common.Interfaces;
using ShopNet.Application.Common.Models;
using ShopNet.Domain.Entities;
using ShopNet.Domain.Enums;

namespace ShopNet.Application.Features.Orders.Commands;

public record ProcessPaymentCallbackCommand(
    PaymentMethod Provider,
    IDictionary<string, string> QueryParams,
    MoMoIpnRequest? MoMoPayload = null
) : IRequest<Result<string>>;

public class ProcessPaymentCallbackCommandHandler : IRequestHandler<ProcessPaymentCallbackCommand, Result<string>>
{
    private readonly IApplicationDbContext _context;
    private readonly IVnPayService _vnPayService;
    private readonly IMoMoService _moMoService;

    public ProcessPaymentCallbackCommandHandler(
        IApplicationDbContext context,
        IVnPayService vnPayService,
        IMoMoService moMoService)
    {
        _context = context;
        _vnPayService = vnPayService;
        _moMoService = moMoService;
    }

    public async Task<Result<string>> Handle(ProcessPaymentCallbackCommand request, CancellationToken cancellationToken)
    {
        if (request.Provider == PaymentMethod.VNPay)
        {
            var vnpayResult = _vnPayService.ProcessIpn(request.QueryParams);
            if (!vnpayResult.IsSuccess && vnpayResult.Message == "Chữ ký không hợp lệ")
            {
                return Result<string>.Failure("Chữ ký VNPay không hợp lệ.");
            }

            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.OrderNumber == vnpayResult.OrderNumber, cancellationToken);

            if (order == null)
            {
                return Result<string>.Failure($"Không tìm thấy đơn hàng {vnpayResult.OrderNumber}");
            }

            var transaction = new PaymentTransaction
            {
                OrderId = order.Id,
                TransactionId = vnpayResult.OrderNumber,
                Provider = PaymentMethod.VNPay,
                Amount = vnpayResult.Amount,
                GatewayTransactionNo = vnpayResult.TransactionNo,
                ResponseCode = vnpayResult.ResponseCode,
                Message = vnpayResult.Message,
                IsSuccess = vnpayResult.IsSuccess
            };
            _context.PaymentTransactions.Add(transaction);

            if (vnpayResult.IsSuccess)
            {
                order.PaymentStatus = PaymentStatus.Completed;
                if (order.Status == OrderStatus.Pending)
                {
                    order.Status = OrderStatus.Processing;
                }
            }
            else
            {
                order.PaymentStatus = PaymentStatus.Failed;
            }

            await _context.SaveChangesAsync(cancellationToken);
            return Result<string>.Success(order.OrderNumber, vnpayResult.Message);
        }
        else if (request.Provider == PaymentMethod.MoMo && request.MoMoPayload != null)
        {
            var momo = request.MoMoPayload;
            bool isSignatureValid = _moMoService.ValidateSignature(momo);
            if (!isSignatureValid)
            {
                return Result<string>.Failure("Chữ ký MoMo không hợp lệ.");
            }

            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.OrderNumber == momo.OrderId, cancellationToken);

            if (order == null)
            {
                return Result<string>.Failure($"Không tìm thấy đơn hàng {momo.OrderId}");
            }

            bool isSuccess = momo.ResultCode == 0;
            var transaction = new PaymentTransaction
            {
                OrderId = order.Id,
                TransactionId = momo.OrderId,
                Provider = PaymentMethod.MoMo,
                Amount = momo.Amount,
                GatewayTransactionNo = momo.TransId.ToString(),
                ResponseCode = momo.ResultCode.ToString(),
                Message = momo.Message,
                IsSuccess = isSuccess
            };
            _context.PaymentTransactions.Add(transaction);

            if (isSuccess)
            {
                order.PaymentStatus = PaymentStatus.Completed;
                if (order.Status == OrderStatus.Pending)
                {
                    order.Status = OrderStatus.Processing;
                }
            }
            else
            {
                order.PaymentStatus = PaymentStatus.Failed;
            }

            await _context.SaveChangesAsync(cancellationToken);
            return Result<string>.Success(order.OrderNumber, momo.Message);
        }

        return Result<string>.Failure("Phương thức thanh toán không hỗ trợ.");
    }
}
