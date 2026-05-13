using System;
using jitu_dashboard.Server.Models;

namespace jitu_dashboard.Server.Repository.PaymentHistory;

public interface IPaymentHistoryRepository
{
    Task<IEnumerable<PaymentHistoryModel>> Retrieve();
}
