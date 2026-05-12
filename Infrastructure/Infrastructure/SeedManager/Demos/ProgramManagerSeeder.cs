// Demo-only fictional network / CRM tickets (not real operator incidents).
using Application.Common.Repositories;
using Application.Features.NumberSequenceManager;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.SeedManager.Demos
{
    public class ProgramManagerSeeder
    {
        private static readonly (string Title, string Summary, ProgramManagerPriority Priority)[] TelecomTickets =
        {
            ("ضعف تغطية في ريف دمشق - ضاحية قدسيا", "بلاغ مشترك: انقطاع متكرر للمكالمات وضعف 4G — يُطلب مسح شبكة وزيارة ميدانية. [Map mock: 33.52N, 36.22E]", ProgramManagerPriority.Critical),
            ("طلب تفعيل باقة سوريا تيليكوم ميكس لموظفي شركة XYZ", "طلب تجاري: تفعيل جماعي — نسخ عقد + قائمة أرقام — بانتظار موافقة القسم المالي.", ProgramManagerPriority.High),
            ("اعتراض على فاتورة جوال - رقم 0933123456", "المشترك يطلب مراجعة بند استهلاك إنترنت لشهر سابق — إرفاق طباعة تفصيلية.", ProgramManagerPriority.Normal),
            ("بطء إنترنت منزلي - حي العزيزية حلب", "شكوى أداء ADSL/راوتر — تم استبدال الراوتر محلياً وجاري متابعة البوابة.", ProgramManagerPriority.High),
            ("طلب تمديد باقة أعمال برو 50GB", "شركة صغيرة: زيادة الحصة قبل نهاية الشهر — تأكيد من المبيعات.", ProgramManagerPriority.Normal),
            ("شريحة جديدة - راوتر 5G لا يتصل", "جهاز جديد من المعرض — إعدادات APN أو تعريف الجهاز على الشبكة.", ProgramManagerPriority.High),
            ("نقل ملكية خط داخل العائلة", "طلب Take Over — وثائق هوية الطرفين مرفوعة — مراجعة المكتب الخلفي.", ProgramManagerPriority.Normal),
            ("طلب إلغاء اشتراك باقة ليلية غير محدودة", "تغيير في احتياجات الاستخدام — إيقاف التجديد التلقائي.", ProgramManagerPriority.Low),
            ("شكوى ضوضاء على خط أرضي - حمص", "تداخل خطي — إحالة لفريق الشبكة الثابتة.", ProgramManagerPriority.Normal),
            ("تفعيل Wingle لموظف ميداني", "مودم USB للعمل عن بُعد — تسليم من مركز خدمة اللاذقية.", ProgramManagerPriority.Low)
        };

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

            var dateFinish = DateTime.Now;
            var dateStart = new DateTime(dateFinish.AddMonths(-6).Year, dateFinish.AddMonths(-6).Month, 1);

            var ticketIndex = 0;

            for (DateTime date = dateStart; date < dateFinish; date = date.AddMonths(1))
            {
                DateTime[] transactionDates = GetRandomDays(date.Year, date.Month, 12, random);

                foreach (DateTime transDate in transactionDates)
                {
                    var number = _numberSequenceService.GenerateNumber(nameof(ProgramManager), "", "PRG");
                    var ticket = TelecomTickets[ticketIndex % TelecomTickets.Length];
                    ticketIndex++;

                    var programManager = new ProgramManager
                    {
                        Title = ticket.Title,
                        Number = number,
                        Summary = ticket.Summary,
                        ProgramManagerResourceId = GetRandomValue(programManagerResources, random),
                        Status = (ProgramManagerStatus)random.Next(0, programStatusLength),
                        Priority = ticket.Priority
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

        private static DateTime[] GetRandomDays(int year, int month, int count, Random random)
        {
            var daysInMonth = Enumerable.Range(1, DateTime.DaysInMonth(year, month)).ToList();
            var selectedDays = new List<int>();

            for (int i = 0; i < count && daysInMonth.Count > 0; i++)
            {
                int day = daysInMonth[random.Next(daysInMonth.Count)];
                selectedDays.Add(day);
                daysInMonth.Remove(day);
            }

            return selectedDays.Select(day => new DateTime(year, month, day)).ToArray();
        }
    }
}
