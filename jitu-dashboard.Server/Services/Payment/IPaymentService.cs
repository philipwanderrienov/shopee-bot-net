using System;
using jitu_dashboard.Server.Models;

namespace jitu_dashboard.Server.Services.Payment;

public interface IPaymentService
{
    Task<IEnumerable<PaymentModel>> Retrieve();
}
