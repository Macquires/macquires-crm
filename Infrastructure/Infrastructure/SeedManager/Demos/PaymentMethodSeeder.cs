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
            new PaymentMethod { Name = "Credit Card" },
            new PaymentMethod { Name = "Debit Card" },
            new PaymentMethod { Name = "Bank Transfer" },
            new PaymentMethod { Name = "PayPal" },
            new PaymentMethod { Name = "Cash" }
        };

        foreach (var paymentMethod in paymentMethods)
        {
            await _repository.CreateAsync(paymentMethod);
        }

        await _unitOfWork.SaveAsync();
    }
}