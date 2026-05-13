using System;
using jitu_dashboard.Server.Models;

namespace jitu_dashboard.Server.Repository.Payment;

public interface IPaymentRepository
{
    Task<IEnumerable<PaymentModel>> Retrieve();
}
