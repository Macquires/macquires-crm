using Application.Common.Audit;
using Application.Common.Repositories;
using Application.Common.Security;
using Infrastructure.Security.FieldEncryption;
using Application.Features.CustomerManager.Commands;
using Application.Features.CustomerManager.Queries;
using Application.Features.NumberSequenceManager;
using Application.Features.TelecomManager.Commands;
using Application.Tests.Dashboard;
using Domain.Common;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using Infrastructure.DataAccessManager.EFCore.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Application.Tests.CustomerOnboarding;

using Application.Tests.TestSupport;

internal static class CustomerOnboardingTestSupport
{
    internal static QueryContext CreateContext()
    {
        DashboardTestEncryption.EnsureInitialized();
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new QueryContext(options, TestOperatorContext.Instance);
    }

    internal static Task<int> BackfillAllMissingAsync(QueryContext ctx)
    {
        var encryption = new PassthroughEncryption();
        var uow = new CtxUnitOfWork(ctx);
        var backfill = new NationalIdSearchHashBackfillService(ctx, encryption, uow);
        return backfill.BackfillAllMissingAsync();
    }

    internal static FindCustomerCandidatesHandler CreateFindHandler(
        QueryContext ctx,
        CaptureSubscriberAudit? audit = null)
    {
        var encryption = new PassthroughEncryption();
        var uow = new CtxUnitOfWork(ctx);
        var backfill = new NationalIdSearchHashBackfillService(ctx, encryption, uow);
        return new FindCustomerCandidatesHandler(ctx, encryption, audit ?? new CaptureSubscriberAudit(), backfill);
    }

    internal static EnsureSubscriberProfileForCustomerHandler CreateEnsureHandler(QueryContext ctx)
    {
        var uow = new CtxUnitOfWork(ctx);
        var repo = new EfCommandRepository<SubscriberProfile>(ctx.SubscriberProfile);
        return new EnsureSubscriberProfileForCustomerHandler(ctx, repo, uow);
    }

    internal static CreateCustomerHandler CreateCreateHandler(QueryContext ctx)
    {
        var uow = new CtxUnitOfWork(ctx);
        var customerRepo = new EfCommandRepository<Customer>(ctx.Customer);
        var profileRepo = new EfCommandRepository<SubscriberProfile>(ctx.SubscriberProfile);
        var seqRepo = new EfCommandRepository<NumberSequence>(ctx.NumberSequence);
        var numberSequence = new NumberSequenceService(seqRepo, uow);
        return new CreateCustomerHandler(
            customerRepo,
            profileRepo,
            uow,
            numberSequence,
            ctx,
            new PassthroughEncryption(),
            new NoopUserAudit());
    }

    internal static CreateCustomerRequest BuildValidIndividualCreateRequest(
        string nationalId = "0107004321",
        string phone = "0937004321",
        string? groupId = "grp-1",
        string? categoryId = "cat-1") =>
        new()
        {
            Name = "مشترك POS",
            Street = "شارع تجريبي",
            City = "دمشق",
            State = "دمشق",
            ZipCode = "10001",
            Country = "SY",
            PhoneNumber = phone,
            EmailAddress = "pos@demo.local",
            CustomerGroupId = groupId,
            CustomerCategoryId = categoryId,
            CreatedById = "pos-user",
            CustomerKind = CustomerKind.Individual,
            NationalId = nationalId,
        };

    /// <summary>Full payload shape sent by TelecomHub customer wizard (aligned with Customer List).</summary>
    internal static CreateCustomerRequest BuildHubWizardIndividualPayload(
        string nationalId = "0107004321",
        string? groupId = "grp-ind",
        string? categoryId = "cat-ind") =>
        new()
        {
            Name = "مشترك POS",
            Street = "شارع تجريبي",
            City = "دمشق",
            State = "دمشق",
            ZipCode = "10001",
            Country = "سوريا",
            PhoneNumber = "0937004321",
            EmailAddress = "pos@demo.local",
            CustomerGroupId = groupId,
            CustomerCategoryId = categoryId,
            CreatedById = "pos-user",
            CustomerKind = CustomerKind.Individual,
            NationalId = nationalId,
        };

    internal static CreateCustomerRequest BuildHubQuickRegisterPayload(string nationalId = "0107004321") =>
        new()
        {
            PosQuickRegister = true,
            Name = "مشترك POS",
            PhoneNumber = "0937004321",
            CreatedById = "pos-user",
            CustomerKind = CustomerKind.Individual,
            NationalId = nationalId,
        };

    internal static async Task SeedPosDefaultsAsync(QueryContext ctx)
    {
        ctx.CustomerGroup.AddRange(
            new CustomerGroup { Id = "grp-ind", Name = "عملاء أفراد — تجزئة" },
            new CustomerGroup { Id = "grp-corp", Name = "شركات — أعمال" });
        ctx.CustomerCategory.AddRange(
            new CustomerCategory { Id = "cat-ind", Name = "أفراد — استهلاك منزلي" },
            new CustomerCategory { Id = "cat-corp", Name = "شركات صغيرة ومتاجر" });
        await ctx.SaveChangesAsync();
    }

    internal static async Task<IndividualCustomer> SeedIndividualAsync(
        QueryContext ctx,
        string nationalId,
        string phone,
        string name = "فرد تجريبي",
        bool isDeleted = false,
        DateTime? createdAtUtc = null)
    {
        var id = Guid.NewGuid().ToString();
        var customer = IndividualCustomer.Create(
            name,
            "ACC-" + id[..8],
            nationalId,
            new PostalAddress("-", "دمشق", "دمشق", "10001", "SY"),
            "test@demo.local",
            phone,
            null,
            null);
        customer.Id = id;
        customer.IsDeleted = isDeleted;
        customer.CreatedAtUtc = createdAtUtc ?? DateTime.UtcNow;
        customer.SetNationalIdSearchHash(nationalId);
        ctx.Customer.Add(customer);
        await ctx.SaveChangesAsync();
        return customer;
    }

    internal static async Task<CorporateCustomer> SeedCorporateAsync(
        QueryContext ctx,
        string commercialRegistry,
        string phone,
        string name = "شركة تجريبية",
        bool isDeleted = false,
        DateTime? createdAtUtc = null)
    {
        var id = Guid.NewGuid().ToString();
        var customer = CorporateCustomer.Create(
            name,
            "ACC-" + id[..8],
            commercialRegistry,
            new PostalAddress("-", "حلب", "حلب", "20002", "SY"),
            "corp@demo.local",
            phone,
            null,
            null);
        customer.Id = id;
        customer.IsDeleted = isDeleted;
        customer.CreatedAtUtc = createdAtUtc ?? DateTime.UtcNow;
        ctx.Customer.Add(customer);
        await ctx.SaveChangesAsync();
        return customer;
    }

    internal static async Task AddActiveLineAsync(QueryContext ctx, string customerId)
    {
        var profile = new SubscriberProfile
        {
            Id = Guid.NewGuid().ToString(),
            CustomerId = customerId,
            IsDeleted = false,
            OperationalStatus = SubscriberOperationalStatus.Active,
        };
        var asset = new MsisdnAsset
        {
            Id = Guid.NewGuid().ToString(),
            Msisdn = "0939" + Random.Shared.Next(1000000, 9999999),
            IsDeleted = false,
            SubscriberProfileId = profile.Id,
        };
        asset.TransitionTo(MsisdnPoolStatus.Active);
        var sub = new TelecomSubscription
        {
            Id = Guid.NewGuid().ToString(),
            SubscriberProfileId = profile.Id,
            MsisdnAssetId = asset.Id,
            IsDeleted = false,
            IsPrimaryLine = true,
        };
        ctx.SubscriberProfile.Add(profile);
        ctx.MsisdnAsset.Add(asset);
        ctx.TelecomSubscription.Add(sub);
        await ctx.SaveChangesAsync();
    }

    internal sealed class NoopUserAudit : IUserAuditService
    {
        public Task LogAsync(UserAuditLogRequest entry, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    internal sealed class PassthroughEncryption : IFieldEncryptionService
    {
        public string Encrypt(string plain) => plain;
        public string Decrypt(string cipher) => cipher;
        public string ComputeSearchHash(string plain) => plain;
    }

    internal sealed class CaptureSubscriberAudit : ISubscriberAccessAuditService
    {
        public List<SearchAuditEntry> Searches { get; } = [];

        public Task LogSearchAsync(
            string searchChannel,
            string? nationalId,
            string? phoneOrMsisdn,
            string? freeTextTerm,
            IReadOnlyList<SubscriberSearchMatchAuditDto> matches,
            CancellationToken cancellationToken = default)
        {
            Searches.Add(new SearchAuditEntry(searchChannel, nationalId, phoneOrMsisdn, matches.Count));
            return Task.CompletedTask;
        }

        public Task LogProfileViewAsync(
            string customerId,
            string? displayName,
            string? primaryPhoneOrMsisdn,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    internal sealed record SearchAuditEntry(string Channel, string? NationalId, string? Phone, int MatchCount);

    internal sealed class CtxUnitOfWork(QueryContext ctx) : IUnitOfWork
    {
        public Task SaveAsync(CancellationToken cancellationToken = default) =>
            ctx.SaveChangesAsync(cancellationToken);

        public void Save() => ctx.SaveChanges();

        public Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    internal sealed class EfCommandRepository<T>(DbSet<T> set) : ICommandRepository<T>
        where T : BaseEntity
    {
        public Task CreateAsync(T entity, CancellationToken cancellationToken = default)
        {
            set.Add(entity);
            return Task.CompletedTask;
        }

        public void Create(T entity) => set.Add(entity);

        public void Update(T entity) => set.Update(entity);

        public void Delete(T entity) => set.Remove(entity);

        public void Purge(T entity) => set.Remove(entity);

        public async Task<T?> GetAsync(string id, CancellationToken cancellationToken = default) =>
            await set.FindAsync([id], cancellationToken);

        public T? Get(string id) => set.Find(id);

        public IQueryable<T> GetQuery() => set;
    }
}
