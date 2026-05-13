using System;
using jitu_dashboard.Server.Models;
using jitu_dashboard.Server.Message.Request;
using jitu_dashboard.Server.Message.Response;
using jitu_dashboard.Server.Repository.Payment;

namespace jitu_dashboard.Server.Services.Payment;

public class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _paymentRepository;

    public PaymentService(IPaymentRepository paymentRepository)
    {
        _paymentRepository = paymentRepository;
    }

    public async Task<IEnumerable<PaymentModel>> Retrieve()
    {
        return await _paymentRepository.Retrieve();
    }
}
