using Application.Common.Repositories;
using Domain.Entities;

namespace Infrastructure.SeedManager.Demos;

public class PaymentMethodSeeder
{
    private readonly ICommandRepository<PaymentMethod> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public PaymentMethodSeeder(
        ICommandRepository<PaymentMethod> repository,
        IUnitOfWork unitOfWork
    )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task GenerateDataAsync()
    {
        var paymentMethods = new List<PaymentMethod>
        {
            new PaymentMethod { Name = "بطاقة بنكية — POS" },
            new PaymentMethod { Name = "حوالة بنكية — ليرة سورية" },
            new PaymentMethod { Name = "كاش — صندوق المعرض" },
            new PaymentMethod { Name = "محفظة إلكترونية — ديمو" },
            new PaymentMethod { Name = "شيك بنكي" }
        };

        foreach (var paymentMethod in paymentMethods)
        {
            await _repository.CreateAsync(paymentMethod);
        }

        await _unitOfWork.SaveAsync();
    }
}
