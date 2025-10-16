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
                new ProgramManagerResource { Name = "Information Technology" },
                new ProgramManagerResource { Name = "Human Resource" },
                new ProgramManagerResource { Name = "Operations" },
                new ProgramManagerResource { Name = "Sales Marketing" },
                new ProgramManagerResource { Name = "Finance Accounting" }
            };

            foreach (var programResource in programResources)
            {
                await _repository.CreateAsync(programResource);
            }

            await _unitOfWork.SaveAsync();
        }
    }
}
