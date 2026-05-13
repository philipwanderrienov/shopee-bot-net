using System;
using jitu_dashboard.Server.Models;

namespace jitu_dashboard.Server.Services.PaymentHistory;

public interface IPaymentHistoryService
{
    Task<IEnumerable<PaymentHistoryModel>> Retrieve();
}
