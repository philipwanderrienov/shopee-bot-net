using System;
using jitu_dashboard.Server.Models;

namespace jitu_dashboard.Server.Repository.PaymentHistory;

public class PaymentHistoryRepository : IPaymentHistoryRepository
{
    private readonly EFRepository.IRepository<PaymentHistoryModel> _repository;

    public PaymentHistoryRepository(EFRepository.IRepository<PaymentHistoryModel> repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<PaymentHistoryModel>> Retrieve()
    {
        IEnumerable<PaymentHistoryModel> listData = new List<PaymentHistoryModel>();

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