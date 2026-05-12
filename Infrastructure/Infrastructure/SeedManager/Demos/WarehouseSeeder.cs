// Demo-only — Syria Telecom regional service hubs (fictional addresses).
using Application.Common.Repositories;
using Domain.Entities;

namespace Infrastructure.SeedManager.Demos
{
    public class WarehouseSeeder
    {
        private readonly ICommandRepository<Warehouse> _repository;
        private readonly IUnitOfWork _unitOfWork;

        public WarehouseSeeder(
            ICommandRepository<Warehouse> repository,
            IUnitOfWork unitOfWork
        )
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task GenerateDataAsync()
        {
            var warehouses = new List<Warehouse>
            {
                new Warehouse { Name = "سوريا تيليكوم — مركز خدمة دمشق (الحجاز)" },
                new Warehouse { Name = "سوريا تيليكوم — مركز خدمة حلب (العزيزية)" },
                new Warehouse { Name = "سوريا تيليكوم — مركز خدمة اللاذقية (المشروع السابع)" },
                new Warehouse { Name = "سوريا تيليكوم — مركز خدمة حمص (المحطة)" }
            };

            foreach (var warehouse in warehouses)
            {
                await _repository.CreateAsync(warehouse);
            }

            await _unitOfWork.SaveAsync();
        }
    }
}
