using Application.Common.Exceptions;
using Application.Features.CustomerManager.Commands;
using Application.Features.CustomerManager.Queries;
using Application.Features.TelecomManager.Commands;
using Domain.Entities;
using Domain.Enums;
using FluentValidation.TestHelper;
using Microsoft.EntityFrameworkCore;
namespace Application.Tests.CustomerOnboarding;

/// <summary>
/// Backend coverage for Telecom Hub / CustomerList POS onboarding wizard (search → create → ensure profile → activate handoff).
/// </summary>
public class CustomerOnboardingWizardTests
{
    #region Validator

    [Fact]
    public void Validator_rejects_when_both_identity_and_phone_too_short()
    {
        var validator = new FindCustomerCandidatesValidator();
        var result = validator.TestValidate(new FindCustomerCandidatesRequest { NationalId = "1", Phone = "0" });
        result.ShouldHaveValidationErrorFor(x => x);
    }

    [Theory]
    [InlineData("01", null)]
    [InlineData(null, "09")]
    [InlineData("0107001234", null)]
    [InlineData("CR", null)]
    [InlineData(null, "0931234567")]
    [InlineData("0107001234", "0931111111")]
    public void Validator_accepts_valid_search_inputs(string? nationalId, string? phone)
    {
        var validator = new FindCustomerCandidatesValidator();
        var result = validator.TestValidate(new FindCustomerCandidatesRequest
        {
            NationalId = nationalId,
            Phone = phone,
        });
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region FindCustomerCandidates — individual (10-digit national ID)

    [Fact]
    public async Task Backfill_skips_duplicate_national_id_without_throwing()
    {
        await using var ctx = CustomerOnboardingTestSupport.CreateContext();
        const string nationalId = "0105315979";

        var first = IndividualCustomer.Create(
            "أول",
            "ACC-1",
            nationalId,
            new Domain.ValueObjects.PostalAddress("-", "دمشق", "دمشق", "10001", "SY"),
            "a@demo.local",
            "0931111111",
            null,
            null);
        first.Id = Guid.NewGuid().ToString();
        first.CreatedAtUtc = DateTime.UtcNow.AddDays(-1);

        var duplicate = IndividualCustomer.Create(
            "ثاني",
            "ACC-2",
            nationalId,
            new Domain.ValueObjects.PostalAddress("-", "حلب", "حلب", "20002", "SY"),
            "b@demo.local",
            "0932222222",
            null,
            null);
        duplicate.Id = Guid.NewGuid().ToString();
        duplicate.CreatedAtUtc = DateTime.UtcNow;

        ctx.Customer.AddRange(first, duplicate);
        await ctx.SaveChangesAsync();

        var backfilled = await CustomerOnboardingTestSupport.BackfillAllMissingAsync(ctx);
        Assert.Equal(1, backfilled);

        var hashes = await ctx.Customer
            .OfType<IndividualCustomer>()
            .Where(c => c.Id == first.Id || c.Id == duplicate.Id)
            .Select(c => new { c.Id, c.NationalIdSearchHash })
            .ToListAsync();

        Assert.False(string.IsNullOrEmpty(hashes.Single(h => h.Id == first.Id).NationalIdSearchHash));
        Assert.Null(hashes.Single(h => h.Id == duplicate.Id).NationalIdSearchHash);
    }

    [Fact]
    public async Task Find_by_national_id_finds_legacy_customer_without_search_hash()
    {
        await using var ctx = CustomerOnboardingTestSupport.CreateContext();
        const string nationalId = "0105315979";
        var customer = IndividualCustomer.Create(
            "مشترك قديم",
            "ACC-LEGACY",
            nationalId,
            new Domain.ValueObjects.PostalAddress("-", "دمشق", "دمشق", "10001", "SY"),
            "legacy@demo.local",
            "0935315979",
            null,
            null);
        customer.Id = Guid.NewGuid().ToString();
        ctx.Customer.Add(customer);
        await ctx.SaveChangesAsync();

        var handler = CustomerOnboardingTestSupport.CreateFindHandler(ctx);
        var result = await handler.Handle(
            new FindCustomerCandidatesRequest { NationalId = nationalId },
            CancellationToken.None);

        Assert.Single(result.Data);
        Assert.Equal(customer.Id, result.Data[0].Id);

        var hash = await ctx.Customer
            .OfType<IndividualCustomer>()
            .Where(c => c.Id == customer.Id)
            .Select(c => c.NationalIdSearchHash)
            .FirstAsync();
        Assert.False(string.IsNullOrEmpty(hash));
    }

    [Fact]
    public async Task Find_by_full_ten_digit_national_id_returns_individual()
    {
        await using var ctx = CustomerOnboardingTestSupport.CreateContext();
        var seeded = await CustomerOnboardingTestSupport.SeedIndividualAsync(ctx, "0107001234", "0931000001", "أحمد POS");

        var handler = CustomerOnboardingTestSupport.CreateFindHandler(ctx);
        var result = await handler.Handle(
            new FindCustomerCandidatesRequest { NationalId = "0107001234" },
            CancellationToken.None);

        Assert.Single(result.Data);
        Assert.Equal(seeded.Id, result.Data[0].Id);
        Assert.Equal("أحمد POS", result.Data[0].Name);
    }

    [Fact]
    public async Task Find_by_nine_digit_national_id_does_not_match_individual()
    {
        await using var ctx = CustomerOnboardingTestSupport.CreateContext();
        await CustomerOnboardingTestSupport.SeedIndividualAsync(ctx, "0107001234", "0931000001");

        var audit = new CustomerOnboardingTestSupport.CaptureSubscriberAudit();
        var handler = CustomerOnboardingTestSupport.CreateFindHandler(ctx, audit);
        var result = await handler.Handle(
            new FindCustomerCandidatesRequest { NationalId = "010700123" },
            CancellationToken.None);

        Assert.Empty(result.Data);
        Assert.Single(audit.Searches);
        Assert.Equal(0, audit.Searches[0].MatchCount);
    }

    [Fact]
    public async Task Find_excludes_deleted_individual_by_national_id()
    {
        await using var ctx = CustomerOnboardingTestSupport.CreateContext();
        await CustomerOnboardingTestSupport.SeedIndividualAsync(ctx, "0107001234", "0931000001", isDeleted: true);

        var audit = new CustomerOnboardingTestSupport.CaptureSubscriberAudit();
        var handler = CustomerOnboardingTestSupport.CreateFindHandler(ctx, audit);
        var result = await handler.Handle(
            new FindCustomerCandidatesRequest { NationalId = "0107001234" },
            CancellationToken.None);

        Assert.Empty(result.Data);
    }

    #endregion

    #region FindCustomerCandidates — corporate (commercial registry partial)

    [Fact]
    public async Task Find_by_partial_commercial_registry_returns_corporate()
    {
        await using var ctx = CustomerOnboardingTestSupport.CreateContext();
        var seeded = await CustomerOnboardingTestSupport.SeedCorporateAsync(ctx, "CR-100200", "0932000002", "شركة النور");

        var handler = CustomerOnboardingTestSupport.CreateFindHandler(ctx);
        var result = await handler.Handle(
            new FindCustomerCandidatesRequest { NationalId = "CR-100" },
            CancellationToken.None);

        Assert.Single(result.Data);
        Assert.Equal(seeded.Id, result.Data[0].Id);
    }

    [Fact]
    public async Task Find_by_exact_commercial_registry_returns_corporate()
    {
        await using var ctx = CustomerOnboardingTestSupport.CreateContext();
        await CustomerOnboardingTestSupport.SeedCorporateAsync(ctx, "CR-100200", "0932000002");

        var handler = CustomerOnboardingTestSupport.CreateFindHandler(ctx);
        var result = await handler.Handle(
            new FindCustomerCandidatesRequest { NationalId = "CR-100200" },
            CancellationToken.None);

        Assert.Single(result.Data);
    }

    #endregion

    #region FindCustomerCandidates — phone

    [Fact]
    public async Task Find_by_phone_only_returns_customer()
    {
        await using var ctx = CustomerOnboardingTestSupport.CreateContext();
        var seeded = await CustomerOnboardingTestSupport.SeedIndividualAsync(ctx, "0108111222", "0938123456");

        var handler = CustomerOnboardingTestSupport.CreateFindHandler(ctx);
        var result = await handler.Handle(
            new FindCustomerCandidatesRequest { Phone = "8123456" },
            CancellationToken.None);

        Assert.Single(result.Data);
        Assert.Equal(seeded.Id, result.Data[0].Id);
    }

    [Fact]
    public async Task Find_by_canonical_msisdn_phone()
    {
        await using var ctx = CustomerOnboardingTestSupport.CreateContext();
        var seeded = await CustomerOnboardingTestSupport.SeedIndividualAsync(ctx, "0109222333", "0939123456");

        var handler = CustomerOnboardingTestSupport.CreateFindHandler(ctx);
        var result = await handler.Handle(
            new FindCustomerCandidatesRequest { Phone = "0939123456" },
            CancellationToken.None);

        Assert.Single(result.Data);
        Assert.Equal(seeded.Id, result.Data[0].Id);
    }

    #endregion

    #region FindCustomerCandidates — combined identity + phone (OR)

    [Fact]
    public async Task Find_by_national_and_phone_matches_either_identity_or_phone()
    {
        await using var ctx = CustomerOnboardingTestSupport.CreateContext();
        var byNat = await CustomerOnboardingTestSupport.SeedIndividualAsync(ctx, "0107001234", "0931000001", "بالهوية");
        var byPhone = await CustomerOnboardingTestSupport.SeedIndividualAsync(ctx, "0107005678", "0939998877", "بالجوال");

        var handler = CustomerOnboardingTestSupport.CreateFindHandler(ctx);
        var result = await handler.Handle(
            new FindCustomerCandidatesRequest { NationalId = "0107001234", Phone = "998877" },
            CancellationToken.None);

        Assert.Equal(2, result.Data.Count);
        Assert.Contains(result.Data, x => x.Id == byNat.Id);
        Assert.Contains(result.Data, x => x.Id == byPhone.Id);
    }

    [Fact]
    public async Task Find_by_national_and_phone_when_identity_misses_falls_back_to_phone_only()
    {
        await using var ctx = CustomerOnboardingTestSupport.CreateContext();
        var match = await CustomerOnboardingTestSupport.SeedIndividualAsync(ctx, "0107009999", "0937776655", "مطابق بالجوال");

        var handler = CustomerOnboardingTestSupport.CreateFindHandler(ctx);
        var result = await handler.Handle(
            new FindCustomerCandidatesRequest { NationalId = "0107001234", Phone = "776655" },
            CancellationToken.None);

        Assert.Single(result.Data);
        Assert.Equal(match.Id, result.Data[0].Id);
    }

    #endregion

    #region FindCustomerCandidates — line count, limit, audit

    [Fact]
    public async Task Find_includes_telecom_subscription_line_count()
    {
        await using var ctx = CustomerOnboardingTestSupport.CreateContext();
        var customer = await CustomerOnboardingTestSupport.SeedIndividualAsync(ctx, "0107001234", "0931000001");
        await CustomerOnboardingTestSupport.AddActiveLineAsync(ctx, customer.Id);
        await CustomerOnboardingTestSupport.AddActiveLineAsync(ctx, customer.Id);

        var handler = CustomerOnboardingTestSupport.CreateFindHandler(ctx);
        var result = await handler.Handle(
            new FindCustomerCandidatesRequest { NationalId = "0107001234" },
            CancellationToken.None);

        Assert.Equal(2, result.Data[0].TelecomSubscriptionLineCount);
    }

    [Fact]
    public async Task Find_limits_results_to_twenty()
    {
        await using var ctx = CustomerOnboardingTestSupport.CreateContext();
        for (var i = 0; i < 25; i++)
        {
            await CustomerOnboardingTestSupport.SeedCorporateAsync(
                ctx,
                $"CR-BULK-{i:D3}",
                $"093{i:D7}",
                createdAtUtc: DateTime.UtcNow.AddMinutes(-i));
        }

        var handler = CustomerOnboardingTestSupport.CreateFindHandler(ctx);
        var result = await handler.Handle(
            new FindCustomerCandidatesRequest { NationalId = "CR-BULK" },
            CancellationToken.None);

        Assert.Equal(20, result.Data.Count);
    }

    [Fact]
    public async Task Find_logs_audit_with_match_count()
    {
        await using var ctx = CustomerOnboardingTestSupport.CreateContext();
        await CustomerOnboardingTestSupport.SeedIndividualAsync(ctx, "0107001234", "0931000001");

        var audit = new CustomerOnboardingTestSupport.CaptureSubscriberAudit();
        var handler = CustomerOnboardingTestSupport.CreateFindHandler(ctx, audit);
        await handler.Handle(new FindCustomerCandidatesRequest { NationalId = "0107001234" }, CancellationToken.None);

        Assert.Single(audit.Searches);
        Assert.Equal("CustomerRegistry", audit.Searches[0].Channel);
        Assert.Equal("0107001234", audit.Searches[0].NationalId);
        Assert.Equal(1, audit.Searches[0].MatchCount);
    }

    [Fact]
    public async Task Find_unknown_national_only_audits_empty_and_returns_no_rows()
    {
        await using var ctx = CustomerOnboardingTestSupport.CreateContext();
        var audit = new CustomerOnboardingTestSupport.CaptureSubscriberAudit();
        var handler = CustomerOnboardingTestSupport.CreateFindHandler(ctx, audit);

        var result = await handler.Handle(
            new FindCustomerCandidatesRequest { NationalId = "0107001234" },
            CancellationToken.None);

        Assert.Empty(result.Data);
        Assert.Single(audit.Searches);
        Assert.Equal(0, audit.Searches[0].MatchCount);
    }

    [Fact]
    public async Task Find_excludes_deleted_corporate_by_registry()
    {
        await using var ctx = CustomerOnboardingTestSupport.CreateContext();
        await CustomerOnboardingTestSupport.SeedCorporateAsync(ctx, "CR-DELETED-01", "0932000003", isDeleted: true);

        var handler = CustomerOnboardingTestSupport.CreateFindHandler(ctx);
        var result = await handler.Handle(
            new FindCustomerCandidatesRequest { NationalId = "CR-DELETED" },
            CancellationToken.None);

        Assert.Empty(result.Data);
    }

    [Fact]
    public async Task Find_by_partial_registry_two_chars_matches_corporate_only()
    {
        await using var ctx = CustomerOnboardingTestSupport.CreateContext();
        await CustomerOnboardingTestSupport.SeedIndividualAsync(ctx, "0107001234", "0931000001");
        var corp = await CustomerOnboardingTestSupport.SeedCorporateAsync(ctx, "XY-999888", "0938887777");

        var handler = CustomerOnboardingTestSupport.CreateFindHandler(ctx);
        var result = await handler.Handle(
            new FindCustomerCandidatesRequest { NationalId = "XY" },
            CancellationToken.None);

        Assert.Single(result.Data);
        Assert.Equal(corp.Id, result.Data[0].Id);
    }

    [Fact]
    public async Task Find_by_international_963_phone_prefix()
    {
        await using var ctx = CustomerOnboardingTestSupport.CreateContext();
        var seeded = await CustomerOnboardingTestSupport.SeedIndividualAsync(ctx, "0109333444", "0939333444");

        var handler = CustomerOnboardingTestSupport.CreateFindHandler(ctx);
        var result = await handler.Handle(
            new FindCustomerCandidatesRequest { Phone = "+963939333444" },
            CancellationToken.None);

        Assert.Single(result.Data);
        Assert.Equal(seeded.Id, result.Data[0].Id);
    }

    [Fact]
    public async Task Find_returns_dto_fields_and_zero_lines_for_new_customer()
    {
        await using var ctx = CustomerOnboardingTestSupport.CreateContext();
        var seeded = await CustomerOnboardingTestSupport.SeedIndividualAsync(
            ctx, "0107444555", "0937444555", "سارة POS", createdAtUtc: DateTime.UtcNow);

        var handler = CustomerOnboardingTestSupport.CreateFindHandler(ctx);
        var result = await handler.Handle(
            new FindCustomerCandidatesRequest { NationalId = "0107444555" },
            CancellationToken.None);

        var row = Assert.Single(result.Data);
        Assert.Equal("سارة POS", row.Name);
        Assert.Equal("0937444555", row.PhoneNumber);
        Assert.Equal("دمشق", row.City);
        Assert.Equal("test@demo.local", row.EmailAddress);
        Assert.Equal(0, row.TelecomSubscriptionLineCount);
        Assert.Equal(seeded.Id, row.Id);
    }

    [Fact]
    public async Task Find_orders_by_newest_created_first_when_capped()
    {
        await using var ctx = CustomerOnboardingTestSupport.CreateContext();
        var oldest = await CustomerOnboardingTestSupport.SeedCorporateAsync(
            ctx, "CR-ORDER-OLD", "0931000001", createdAtUtc: DateTime.UtcNow.AddDays(-2));
        var newest = await CustomerOnboardingTestSupport.SeedCorporateAsync(
            ctx, "CR-ORDER-NEW", "0931000002", createdAtUtc: DateTime.UtcNow);

        var handler = CustomerOnboardingTestSupport.CreateFindHandler(ctx);
        var result = await handler.Handle(
            new FindCustomerCandidatesRequest { NationalId = "CR-ORDER" },
            CancellationToken.None);

        Assert.Equal(2, result.Data.Count);
        Assert.Equal(newest.Id, result.Data[0].Id);
        Assert.Equal(oldest.Id, result.Data[1].Id);
    }

    [Fact]
    public async Task Find_handler_returns_empty_without_audit_when_both_inputs_too_short()
    {
        await using var ctx = CustomerOnboardingTestSupport.CreateContext();
        await CustomerOnboardingTestSupport.SeedIndividualAsync(ctx, "0107001234", "0931000001");

        var audit = new CustomerOnboardingTestSupport.CaptureSubscriberAudit();
        var handler = CustomerOnboardingTestSupport.CreateFindHandler(ctx, audit);
        var result = await handler.Handle(
            new FindCustomerCandidatesRequest { NationalId = "1", Phone = "0" },
            CancellationToken.None);

        Assert.Empty(result.Data);
        Assert.Empty(audit.Searches);
    }

    [Fact]
    public async Task Find_combined_search_does_not_return_unrelated_customers()
    {
        await using var ctx = CustomerOnboardingTestSupport.CreateContext();
        var match = await CustomerOnboardingTestSupport.SeedIndividualAsync(ctx, "0107001234", "0931111111");
        await CustomerOnboardingTestSupport.SeedIndividualAsync(ctx, "0107999888", "0932222222");

        var handler = CustomerOnboardingTestSupport.CreateFindHandler(ctx);
        var result = await handler.Handle(
            new FindCustomerCandidatesRequest { NationalId = "0107001234", Phone = "2222222" },
            CancellationToken.None);

        Assert.Equal(2, result.Data.Count);
        Assert.Contains(result.Data, x => x.Id == match.Id);
    }

    #endregion

    #region EnsureSubscriberProfileForCustomer — activate handoff

    [Fact]
    public async Task Ensure_creates_profile_when_customer_has_none()
    {
        await using var ctx = CustomerOnboardingTestSupport.CreateContext();
        var customer = await CustomerOnboardingTestSupport.SeedIndividualAsync(ctx, "0105566778", "0935551234");

        var handler = CustomerOnboardingTestSupport.CreateEnsureHandler(ctx);
        var result = await handler.Handle(
            new EnsureSubscriberProfileForCustomerRequest { CustomerId = customer.Id, CreatedById = "user-1" },
            CancellationToken.None);

        Assert.True(result.Created);
        Assert.False(string.IsNullOrWhiteSpace(result.SubscriberProfileId));
        var count = await ctx.SubscriberProfile.CountAsync(p => p.CustomerId == customer.Id && !p.IsDeleted);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task Ensure_returns_existing_profile_without_duplicate()
    {
        await using var ctx = CustomerOnboardingTestSupport.CreateContext();
        var customer = await CustomerOnboardingTestSupport.SeedIndividualAsync(ctx, "0105566778", "0935551234");
        var existingId = Guid.NewGuid().ToString();
        ctx.SubscriberProfile.Add(new Domain.Entities.SubscriberProfile
        {
            Id = existingId,
            CustomerId = customer.Id,
            IsDeleted = false,
        });
        await ctx.SaveChangesAsync();

        var handler = CustomerOnboardingTestSupport.CreateEnsureHandler(ctx);
        var result = await handler.Handle(
            new EnsureSubscriberProfileForCustomerRequest { CustomerId = customer.Id },
            CancellationToken.None);

        Assert.False(result.Created);
        Assert.Equal(existingId, result.SubscriberProfileId);
        Assert.Equal(1, await ctx.SubscriberProfile.CountAsync(p => p.CustomerId == customer.Id));
    }

    [Fact]
    public async Task Ensure_throws_when_customer_missing()
    {
        await using var ctx = CustomerOnboardingTestSupport.CreateContext();
        var handler = CustomerOnboardingTestSupport.CreateEnsureHandler(ctx);

        var ex = await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
            handler.Handle(
                new EnsureSubscriberProfileForCustomerRequest { CustomerId = Guid.NewGuid().ToString() },
                CancellationToken.None));

        Assert.Contains("غير موجود", ex.Message);
    }

    [Fact]
    public void Ensure_validator_requires_customer_id()
    {
        var validator = new EnsureSubscriberProfileForCustomerValidator();
        var result = validator.TestValidate(new EnsureSubscriberProfileForCustomerRequest { CustomerId = "" });
        result.ShouldHaveValidationErrorFor(x => x.CustomerId);
    }

    [Fact]
    public async Task Ensure_throws_when_customer_is_deleted()
    {
        await using var ctx = CustomerOnboardingTestSupport.CreateContext();
        var customer = await CustomerOnboardingTestSupport.SeedIndividualAsync(
            ctx, "0107666777", "0937666777", isDeleted: true);

        var handler = CustomerOnboardingTestSupport.CreateEnsureHandler(ctx);
        var ex = await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
            handler.Handle(
                new EnsureSubscriberProfileForCustomerRequest { CustomerId = customer.Id },
                CancellationToken.None));

        Assert.Contains("غير موجود", ex.Message);
    }

    [Fact]
    public async Task Ensure_creates_new_profile_when_only_deleted_profiles_exist()
    {
        await using var ctx = CustomerOnboardingTestSupport.CreateContext();
        var customer = await CustomerOnboardingTestSupport.SeedIndividualAsync(ctx, "0107888999", "0937888999");
        ctx.SubscriberProfile.Add(new Domain.Entities.SubscriberProfile
        {
            Id = Guid.NewGuid().ToString(),
            CustomerId = customer.Id,
            IsDeleted = true,
        });
        await ctx.SaveChangesAsync();

        var handler = CustomerOnboardingTestSupport.CreateEnsureHandler(ctx);
        var result = await handler.Handle(
            new EnsureSubscriberProfileForCustomerRequest { CustomerId = customer.Id },
            CancellationToken.None);

        Assert.True(result.Created);
        Assert.Equal(1, await ctx.SubscriberProfile.CountAsync(p => p.CustomerId == customer.Id && !p.IsDeleted));
    }

    #endregion

    #region CreateCustomer — wizard save step

    [Fact]
    public void CreateCustomer_validator_rejects_incomplete_hub_payload()
    {
        var validator = new CreateCustomerValidator();
        var payload = new CreateCustomerRequest
        {
            Name = "مشترك POS",
            CustomerKind = CustomerKind.Individual,
            NationalId = "0107004321",
        };
        var result = validator.TestValidate(payload);
        result.ShouldHaveValidationErrorFor(x => x.Street);
        result.ShouldHaveValidationErrorFor(x => x.City);
        result.ShouldHaveValidationErrorFor(x => x.CustomerGroupId);
        result.ShouldHaveValidationErrorFor(x => x.CustomerCategoryId);
    }

    [Fact]
    public void CreateCustomer_validator_accepts_full_hub_wizard_payload()
    {
        var validator = new CreateCustomerValidator();
        var result = validator.TestValidate(CustomerOnboardingTestSupport.BuildHubWizardIndividualPayload());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void CreateCustomer_validator_accepts_full_hub_payload_without_phone()
    {
        var validator = new CreateCustomerValidator();
        var payload = CustomerOnboardingTestSupport.BuildHubWizardIndividualPayload();
        payload.PhoneNumber = null;
        var result = validator.TestValidate(payload);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void CreateCustomer_validator_accepts_full_hub_payload_without_phone_or_email()
    {
        var validator = new CreateCustomerValidator();
        var payload = CustomerOnboardingTestSupport.BuildHubWizardIndividualPayload();
        payload.PhoneNumber = null;
        payload.EmailAddress = null;
        var result = validator.TestValidate(payload);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task CreateCustomer_pos_quick_register_normalizes_then_passes_validation()
    {
        await using var ctx = CustomerOnboardingTestSupport.CreateContext();
        await CustomerOnboardingTestSupport.SeedPosDefaultsAsync(ctx);

        var payload = CustomerOnboardingTestSupport.BuildHubQuickRegisterPayload();
        await CreateCustomerPosQuickRegisterNormalizer.ApplyAsync(payload, ctx);

        var validator = new CreateCustomerValidator();
        var result = validator.TestValidate(payload);
        result.ShouldNotHaveAnyValidationErrors();
        Assert.Equal("grp-ind", payload.CustomerGroupId);
        Assert.Equal("cat-ind", payload.CustomerCategoryId);
        Assert.False(string.IsNullOrWhiteSpace(payload.EmailAddress));
    }

    [Fact]
    public async Task CreateCustomer_pos_quick_register_allows_null_phone()
    {
        await using var ctx = CustomerOnboardingTestSupport.CreateContext();
        await CustomerOnboardingTestSupport.SeedPosDefaultsAsync(ctx);

        var payload = CustomerOnboardingTestSupport.BuildHubQuickRegisterPayload();
        payload.PhoneNumber = null;
        await CreateCustomerPosQuickRegisterNormalizer.ApplyAsync(payload, ctx);

        var validator = new CreateCustomerValidator();
        var result = validator.TestValidate(payload);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task CreateCustomer_individual_then_find_and_ensure_idempotent()
    {
        await using var ctx = CustomerOnboardingTestSupport.CreateContext();
        const string nationalId = "0107111222";
        var create = CustomerOnboardingTestSupport.CreateCreateHandler(ctx);
        var created = await create.Handle(
            CustomerOnboardingTestSupport.BuildValidIndividualCreateRequest(nationalId),
            CancellationToken.None);

        Assert.NotNull(created.Data);
        var profileCountAfterCreate = await ctx.SubscriberProfile.CountAsync(p => p.CustomerId == created.Data!.Id);
        Assert.Equal(1, profileCountAfterCreate);

        var find = CustomerOnboardingTestSupport.CreateFindHandler(ctx);
        var found = await find.Handle(
            new FindCustomerCandidatesRequest { NationalId = nationalId },
            CancellationToken.None);
        Assert.Single(found.Data);

        var ensure = CustomerOnboardingTestSupport.CreateEnsureHandler(ctx);
        var ensured = await ensure.Handle(
            new EnsureSubscriberProfileForCustomerRequest { CustomerId = created.Data.Id },
            CancellationToken.None);
        Assert.False(ensured.Created);
        Assert.Equal(1, await ctx.SubscriberProfile.CountAsync(p => p.CustomerId == created.Data.Id && !p.IsDeleted));
    }

    [Fact]
    public async Task CreateCustomer_rejects_duplicate_national_id()
    {
        await using var ctx = CustomerOnboardingTestSupport.CreateContext();
        const string nationalId = "0107333444";
        var create = CustomerOnboardingTestSupport.CreateCreateHandler(ctx);
        await create.Handle(CustomerOnboardingTestSupport.BuildValidIndividualCreateRequest(nationalId), CancellationToken.None);

        var ex = await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
            create.Handle(CustomerOnboardingTestSupport.BuildValidIndividualCreateRequest(nationalId, "0937333999"), CancellationToken.None));

        Assert.Contains("الرقم الوطني مسجّل", ex.Message);
    }

    #endregion

    #region POS wizard flow (backend contract)

    [Fact]
    public async Task PosFlow_search_miss_then_ensure_profile_for_new_customer()
    {
        await using var ctx = CustomerOnboardingTestSupport.CreateContext();
        const string nationalId = "0107001234";
        var audit = new CustomerOnboardingTestSupport.CaptureSubscriberAudit();
        var find = CustomerOnboardingTestSupport.CreateFindHandler(ctx, audit);

        var search = await find.Handle(
            new FindCustomerCandidatesRequest { NationalId = nationalId },
            CancellationToken.None);
        Assert.Empty(search.Data);

        var customer = await CustomerOnboardingTestSupport.SeedIndividualAsync(ctx, nationalId, "0937001234", "POS جديد");
        var ensure = CustomerOnboardingTestSupport.CreateEnsureHandler(ctx);
        var profile = await ensure.Handle(
            new EnsureSubscriberProfileForCustomerRequest { CustomerId = customer.Id, CreatedById = "pos-user" },
            CancellationToken.None);

        Assert.True(profile.Created);
        Assert.False(string.IsNullOrWhiteSpace(profile.SubscriberProfileId));

        var searchAgain = await find.Handle(
            new FindCustomerCandidatesRequest { NationalId = nationalId },
            CancellationToken.None);
        Assert.Single(searchAgain.Data);
        Assert.Equal(customer.Id, searchAgain.Data[0].Id);
        Assert.Equal(2, audit.Searches.Count);
    }

    [Fact]
    public async Task PosFlow_existing_customer_search_then_ensure_idempotent_for_activate_handoff()
    {
        await using var ctx = CustomerOnboardingTestSupport.CreateContext();
        var customer = await CustomerOnboardingTestSupport.SeedIndividualAsync(ctx, "0101234567", "0931234567", "محمد علي");
        await CustomerOnboardingTestSupport.AddActiveLineAsync(ctx, customer.Id);

        var find = CustomerOnboardingTestSupport.CreateFindHandler(ctx);
        var found = await find.Handle(
            new FindCustomerCandidatesRequest { NationalId = "0101234567" },
            CancellationToken.None);
        Assert.Single(found.Data);
        Assert.Equal(1, found.Data[0].TelecomSubscriptionLineCount);

        var ensure = CustomerOnboardingTestSupport.CreateEnsureHandler(ctx);
        var first = await ensure.Handle(
            new EnsureSubscriberProfileForCustomerRequest { CustomerId = customer.Id },
            CancellationToken.None);
        var second = await ensure.Handle(
            new EnsureSubscriberProfileForCustomerRequest { CustomerId = customer.Id },
            CancellationToken.None);

        Assert.Equal(first.SubscriberProfileId, second.SubscriberProfileId);
        Assert.False(first.Created);
        Assert.False(second.Created);
        Assert.Equal(1, await ctx.SubscriberProfile.CountAsync(p => p.CustomerId == customer.Id && !p.IsDeleted));
    }

    [Fact]
    public async Task PosFlow_corporate_registry_search_then_ensure_profile()
    {
        await using var ctx = CustomerOnboardingTestSupport.CreateContext();
        var corp = await CustomerOnboardingTestSupport.SeedCorporateAsync(ctx, "CR-DEMO-900", "0939000111", "مؤسسة POS");

        var find = CustomerOnboardingTestSupport.CreateFindHandler(ctx);
        var found = await find.Handle(
            new FindCustomerCandidatesRequest { NationalId = "DEMO-9" },
            CancellationToken.None);
        Assert.Single(found.Data);
        Assert.Equal(corp.Id, found.Data[0].Id);

        var ensure = CustomerOnboardingTestSupport.CreateEnsureHandler(ctx);
        var profile = await ensure.Handle(
            new EnsureSubscriberProfileForCustomerRequest { CustomerId = corp.Id },
            CancellationToken.None);
        Assert.True(profile.Created);
    }

    #endregion
}
