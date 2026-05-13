using System;
using jitu_dashboard.Server.Models;
using jitu_dashboard.Server.Message.Request;
using jitu_dashboard.Server.Repository.PaymentHistory;


namespace jitu_dashboard.Server.Services.PaymentHistory;

public class PaymentHistoryService : IPaymentHistoryService
{
    private readonly IPaymentHistoryRepository _paymentHistoryRepository;

    public PaymentHistoryService(IPaymentHistoryRepository paymentHistoryRepository)
    {
        _paymentHistoryRepository = paymentHistoryRepository;
    }

    public async Task<IEnumerable<PaymentHistoryModel>> Retrieve()
    {
        return await _paymentHistoryRepository.Retrieve();
    }
}
