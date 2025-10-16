using Application.Common.Repositories;
using Application.Features.NumberSequenceManager;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.SeedManager.Demos
{
    public class ProgramManagerSeeder
    {
        private readonly ICommandRepository<ProgramManager> _programManagerRepository;
        private readonly ICommandRepository<ProgramManagerResource> _programManagerResourceRepository;
        private readonly NumberSequenceService _numberSequenceService;
        private readonly IUnitOfWork _unitOfWork;

        public ProgramManagerSeeder(
            ICommandRepository<ProgramManager> programManagerRepository,
            ICommandRepository<ProgramManagerResource> programManagerResourceRepository,
            NumberSequenceService numberSequenceService,
            IUnitOfWork unitOfWork
        )
        {
            _programManagerRepository = programManagerRepository;
            _programManagerResourceRepository = programManagerResourceRepository;
            _numberSequenceService = numberSequenceService;
            _unitOfWork = unitOfWork;
        }

        public async Task GenerateDataAsync()
        {
            var random = new Random();
            var programManagerResources = await _programManagerResourceRepository.GetQuery().Select(x => x.Id).ToListAsync();

            int programStatusLength = Enum.GetNames(typeof(ProgramManagerStatus)).Length;
            int programPriorityLength = Enum.GetNames(typeof(ProgramManagerPriority)).Length;

            var dateFinish = DateTime.Now;
            var dateStart = new DateTime(dateFinish.AddMonths(-6).Year, dateFinish.AddMonths(-6).Month, 1);

            for (DateTime date = dateStart; date < dateFinish; date = date.AddMonths(1))
            {
                DateTime[] transactionDates = GetRandomDays(date.Year, date.Month, 12);

                foreach (DateTime transDate in transactionDates)
                {
                    var number = _numberSequenceService.GenerateNumber(nameof(ProgramManager), "", "PRG");

                    var programManager = new ProgramManager
                    {
                        Title = number,
                        Number = number,
                        Summary = $"Lorem Ipsum Dolor Sit Amet Program - #{Guid.NewGuid().ToString().Substring(0, 5)}",
                        ProgramManagerResourceId = GetRandomValue(programManagerResources, random),
                        Status = (ProgramManagerStatus)random.Next(0, programStatusLength),
                        Priority = (ProgramManagerPriority)random.Next(0, programPriorityLength)
                    };

                    await _programManagerRepository.CreateAsync(programManager);
                    await _unitOfWork.SaveAsync();
                }
            }
        }

        private static T GetRandomValue<T>(List<T> list, Random random)
        {
            return list[random.Next(list.Count)];
        }

        private static DateTime[] GetRandomDays(int year, int month, int count)
        {
            var random = new Random();
            var daysInMonth = Enumerable.Range(1, DateTime.DaysInMonth(year, month)).ToList();
            var selectedDays = new List<int>();

            for (int i = 0; i < count; i++)
            {
                int day = daysInMonth[random.Next(daysInMonth.Count)];
                selectedDays.Add(day);
                daysInMonth.Remove(day);
            }

            return selectedDays.Select(day => new DateTime(year, month, day)).ToArray();
        }
    }
}
