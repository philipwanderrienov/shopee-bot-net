using System;
using jitu_dashboard.Server.Models;

namespace jitu_dashboard.Server.Repository.Payment;

public class PaymentRepository : IPaymentRepository
{
    private readonly EFRepository.IRepository<PaymentModel> _repository;

    public PaymentRepository(EFRepository.IRepository<PaymentModel> repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<PaymentModel>> Retrieve()
    {
        IEnumerable<PaymentModel> listData = new List<PaymentModel>();

        try
        {
            listData = await _repository.GetAllAsync();
            return listData;
        }
        catch (Exception ex)
        {
            throw new ApplicationException(ex.Message);
        }
    }
}
