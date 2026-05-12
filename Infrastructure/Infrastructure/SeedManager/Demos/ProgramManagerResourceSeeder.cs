// Demo-only — internal teams for Syria Telecom Kanban demo.
using Application.Common.Repositories;
using Domain.Entities;

namespace Infrastructure.SeedManager.Demos
{
    public class ProgramManagerResourceSeeder
    {
        private readonly ICommandRepository<ProgramManagerResource> _repository;
        private readonly IUnitOfWork _unitOfWork;

        public ProgramManagerResourceSeeder(
            ICommandRepository<ProgramManagerResource> repository,
            IUnitOfWork unitOfWork
        )
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task GenerateDataAsync()
        {
            var programResources = new List<ProgramManagerResource>
            {
                new ProgramManagerResource { Name = "شبكة الوصول — Access" },
                new ProgramManagerResource { Name = "الشبكة الأساسية — Core" },
                new ProgramManagerResource { Name = "تشغيل الميدان — NOC" },
                new ProgramManagerResource { Name = "مبيعات الأعمال — B2B" },
                new ProgramManagerResource { Name = "المالية والتحصيل" }
            };

            foreach (var programResource in programResources)
            {
                await _repository.CreateAsync(programResource);
            }

            await _unitOfWork.SaveAsync();
        }
    }
}
