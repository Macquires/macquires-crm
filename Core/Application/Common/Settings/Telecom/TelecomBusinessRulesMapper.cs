namespace Application.Common.Settings.Telecom;

public static class TelecomBusinessRulesMapper
{
    public static TelecomBusinessRulesDto FromDictionary(IReadOnlyDictionary<string, string> map)
    {
        var dto = new TelecomBusinessRulesDto();

        dto.Inventory.MsisdnQuarantineDays = ReadInt(map, GlobalSettingKeys.TelecomInventoryMsisdnQuarantineDays, dto.Inventory.MsisdnQuarantineDays);
        dto.Inventory.SimQuarantineDays = ReadInt(map, GlobalSettingKeys.TelecomInventorySimQuarantineDays, dto.Inventory.SimQuarantineDays);
        dto.Inventory.MsisdnReservationMinutes = ReadInt(map, GlobalSettingKeys.TelecomInventoryMsisdnReservationMinutes, dto.Inventory.MsisdnReservationMinutes);
        dto.Inventory.MsisdnReservationHours = ReadInt(map, GlobalSettingKeys.TelecomInventoryMsisdnReservationHours, dto.Inventory.MsisdnReservationHours);
        dto.Inventory.DormantLineScanIntervalHours = ReadInt(map, GlobalSettingKeys.TelecomInventoryDormantLineScanIntervalHours, dto.Inventory.DormantLineScanIntervalHours);
        dto.Inventory.DormantLineInactivityDays = ReadInt(map, GlobalSettingKeys.TelecomInventoryDormantLineInactivityDays, dto.Inventory.DormantLineInactivityDays);

        dto.Activation.MaxLinesIndividual = ReadInt(map, GlobalSettingKeys.TelecomActivationMaxLinesIndividual,
            ReadInt(map, GlobalSettingKeys.TelecomMaxActiveLinesPerIndividual, dto.Activation.MaxLinesIndividual));
        dto.Activation.MaxLinesCorporate = ReadInt(map, GlobalSettingKeys.TelecomActivationMaxLinesCorporate, dto.Activation.MaxLinesCorporate);
        dto.Activation.DealerCodeRequired = ReadBool(map, GlobalSettingKeys.TelecomActivationDealerCodeRequired, dto.Activation.DealerCodeRequired);
        dto.Activation.RequireKycBeforeConfirm = ReadBool(map, GlobalSettingKeys.TelecomActivationRequireKycBeforeConfirm, dto.Activation.RequireKycBeforeConfirm);
        dto.Activation.RequirePaymentReferenceWhenDepositDue = ReadBool(map, GlobalSettingKeys.TelecomActivationRequirePaymentReferenceWhenDepositDue, dto.Activation.RequirePaymentReferenceWhenDepositDue);
        dto.Activation.ChannelShowroomLabelAr = ReadString(map, GlobalSettingKeys.TelecomActivationChannelShowroomLabelAr, dto.Activation.ChannelShowroomLabelAr);
        dto.Activation.ChannelShowroomLabelEn = ReadString(map, GlobalSettingKeys.TelecomActivationChannelShowroomLabelEn, dto.Activation.ChannelShowroomLabelEn);
        dto.Activation.ChannelDealerLabelAr = ReadString(map, GlobalSettingKeys.TelecomActivationChannelDealerLabelAr, dto.Activation.ChannelDealerLabelAr);
        dto.Activation.ChannelDealerLabelEn = ReadString(map, GlobalSettingKeys.TelecomActivationChannelDealerLabelEn, dto.Activation.ChannelDealerLabelEn);
        dto.Activation.ChannelDigitalLabelAr = ReadString(map, GlobalSettingKeys.TelecomActivationChannelDigitalLabelAr, dto.Activation.ChannelDigitalLabelAr);
        dto.Activation.ChannelDigitalLabelEn = ReadString(map, GlobalSettingKeys.TelecomActivationChannelDigitalLabelEn, dto.Activation.ChannelDigitalLabelEn);

        dto.Termination.DefaultFinalBillAmountSyp = ReadDecimal(map, GlobalSettingKeys.TelecomTerminationDefaultFinalBillAmountSyp, dto.Termination.DefaultFinalBillAmountSyp);
        dto.Termination.BackOfficeTypes = ReadString(map, GlobalSettingKeys.TelecomTerminationBackOfficeTypes, dto.Termination.BackOfficeTypes);
        dto.Termination.CorporateRequiresBackOffice = ReadBool(map, GlobalSettingKeys.TelecomTerminationCorporateRequiresBackOffice, dto.Termination.CorporateRequiresBackOffice);
        dto.Termination.VoluntaryRequiresRetentionOutcome = ReadBool(map, GlobalSettingKeys.TelecomTerminationVoluntaryRequiresRetentionOutcome, dto.Termination.VoluntaryRequiresRetentionOutcome);

        dto.Refund.DualApprovalThresholdSyp = ReadDecimal(map, GlobalSettingKeys.TelecomRefundDualApprovalThresholdSyp, dto.Refund.DualApprovalThresholdSyp);
        dto.Refund.HighRiskMethods = ReadString(map, GlobalSettingKeys.TelecomRefundHighRiskMethods, dto.Refund.HighRiskMethods);

        dto.Suspension.LongReviewDays = ReadInt(map, GlobalSettingKeys.TelecomSuspensionLongReviewDays, dto.Suspension.LongReviewDays);
        dto.Suspension.BackOfficeTypes = ReadString(map, GlobalSettingKeys.TelecomSuspensionBackOfficeTypes, dto.Suspension.BackOfficeTypes);
        dto.Suspension.CorporateRequiresBackOffice = ReadBool(map, GlobalSettingKeys.TelecomSuspensionCorporateRequiresBackOffice, dto.Suspension.CorporateRequiresBackOffice);

        dto.SimSwap.LostStolenRequiresBackOffice = ReadBool(map, GlobalSettingKeys.TelecomSimSwapLostStolenRequiresBackOffice, dto.SimSwap.LostStolenRequiresBackOffice);

        dto.ChangeNumber.PremiumCategories = ReadString(map, GlobalSettingKeys.TelecomChangeNumberPremiumCategories, dto.ChangeNumber.PremiumCategories);
        dto.ChangeNumber.BlockQuarantinedTarget = ReadBool(map, GlobalSettingKeys.TelecomChangeNumberBlockQuarantinedTarget, dto.ChangeNumber.BlockQuarantinedTarget);

        dto.TakeOver.AlwaysRequiresBackOffice = ReadBool(map, GlobalSettingKeys.TelecomTakeOverAlwaysRequiresBackOffice, dto.TakeOver.AlwaysRequiresBackOffice);
        dto.TakeOver.DefaultDepositTransferPolicy = ReadString(map, GlobalSettingKeys.TelecomTakeOverDefaultDepositTransferPolicy, dto.TakeOver.DefaultDepositTransferPolicy);

        dto.DeviceSales.CreditScoreLowThreshold = ReadInt(map, GlobalSettingKeys.TelecomDeviceSalesCreditScoreLowThreshold, dto.DeviceSales.CreditScoreLowThreshold);
        dto.DeviceSales.CreditScoreGoodThreshold = ReadInt(map, GlobalSettingKeys.TelecomDeviceSalesCreditScoreGoodThreshold, dto.DeviceSales.CreditScoreGoodThreshold);
        dto.DeviceSales.DownPaymentPercentReject = ReadDecimal(map, GlobalSettingKeys.TelecomDeviceSalesDownPaymentPercentReject, dto.DeviceSales.DownPaymentPercentReject);
        dto.DeviceSales.DownPaymentPercentVip = ReadDecimal(map, GlobalSettingKeys.TelecomDeviceSalesDownPaymentPercentVip, dto.DeviceSales.DownPaymentPercentVip);
        dto.DeviceSales.DownPaymentPercentLow = ReadDecimal(map, GlobalSettingKeys.TelecomDeviceSalesDownPaymentPercentLow, dto.DeviceSales.DownPaymentPercentLow);
        dto.DeviceSales.DownPaymentPercentGood = ReadDecimal(map, GlobalSettingKeys.TelecomDeviceSalesDownPaymentPercentGood, dto.DeviceSales.DownPaymentPercentGood);

        dto.BadDebt.PostpaidBadDebtThresholdMonths = ReadInt(map, GlobalSettingKeys.TelecomPostpaidBadDebtThresholdMonths, dto.BadDebt.PostpaidBadDebtThresholdMonths);

        return dto;
    }

    public static IReadOnlyDictionary<string, (string Value, string Category)> ToEntries(TelecomBusinessRulesDto telecom)
    {
        var entries = new Dictionary<string, (string Value, string Category)>(StringComparer.OrdinalIgnoreCase);

        void Add(string key, string value)
        {
            var def = TelecomBusinessRulesCatalog.FindByKey(key);
            entries[key] = (value, def?.Category ?? "Telecom");
        }

        Add(GlobalSettingKeys.TelecomInventoryMsisdnQuarantineDays, telecom.Inventory.MsisdnQuarantineDays.ToString());
        Add(GlobalSettingKeys.TelecomInventorySimQuarantineDays, telecom.Inventory.SimQuarantineDays.ToString());
        Add(GlobalSettingKeys.TelecomInventoryMsisdnReservationMinutes, telecom.Inventory.MsisdnReservationMinutes.ToString());
        Add(GlobalSettingKeys.TelecomInventoryMsisdnReservationHours, telecom.Inventory.MsisdnReservationHours.ToString());
        Add(GlobalSettingKeys.TelecomInventoryDormantLineScanIntervalHours, telecom.Inventory.DormantLineScanIntervalHours.ToString());
        Add(GlobalSettingKeys.TelecomInventoryDormantLineInactivityDays, telecom.Inventory.DormantLineInactivityDays.ToString());
        Add(GlobalSettingKeys.TelecomSuspensionLongReviewDays, telecom.Suspension.LongReviewDays.ToString());

        Add(GlobalSettingKeys.TelecomActivationMaxLinesIndividual, telecom.Activation.MaxLinesIndividual.ToString());
        Add(GlobalSettingKeys.TelecomActivationMaxLinesCorporate, telecom.Activation.MaxLinesCorporate.ToString());
        Add(GlobalSettingKeys.TelecomActivationDealerCodeRequired, telecom.Activation.DealerCodeRequired.ToString().ToLowerInvariant());
        Add(GlobalSettingKeys.TelecomActivationRequireKycBeforeConfirm, telecom.Activation.RequireKycBeforeConfirm.ToString().ToLowerInvariant());
        Add(GlobalSettingKeys.TelecomActivationRequirePaymentReferenceWhenDepositDue, telecom.Activation.RequirePaymentReferenceWhenDepositDue.ToString().ToLowerInvariant());
        Add(GlobalSettingKeys.TelecomActivationChannelShowroomLabelAr, telecom.Activation.ChannelShowroomLabelAr ?? "");
        Add(GlobalSettingKeys.TelecomActivationChannelShowroomLabelEn, telecom.Activation.ChannelShowroomLabelEn ?? "");
        Add(GlobalSettingKeys.TelecomActivationChannelDealerLabelAr, telecom.Activation.ChannelDealerLabelAr ?? "");
        Add(GlobalSettingKeys.TelecomActivationChannelDealerLabelEn, telecom.Activation.ChannelDealerLabelEn ?? "");
        Add(GlobalSettingKeys.TelecomActivationChannelDigitalLabelAr, telecom.Activation.ChannelDigitalLabelAr ?? "");
        Add(GlobalSettingKeys.TelecomActivationChannelDigitalLabelEn, telecom.Activation.ChannelDigitalLabelEn ?? "");

        Add(GlobalSettingKeys.TelecomTerminationDefaultFinalBillAmountSyp, telecom.Termination.DefaultFinalBillAmountSyp.ToString(System.Globalization.CultureInfo.InvariantCulture));
        Add(GlobalSettingKeys.TelecomTerminationBackOfficeTypes, telecom.Termination.BackOfficeTypes ?? "");
        Add(GlobalSettingKeys.TelecomTerminationCorporateRequiresBackOffice, telecom.Termination.CorporateRequiresBackOffice.ToString().ToLowerInvariant());
        Add(GlobalSettingKeys.TelecomTerminationVoluntaryRequiresRetentionOutcome, telecom.Termination.VoluntaryRequiresRetentionOutcome.ToString().ToLowerInvariant());

        Add(GlobalSettingKeys.TelecomRefundDualApprovalThresholdSyp, telecom.Refund.DualApprovalThresholdSyp.ToString(System.Globalization.CultureInfo.InvariantCulture));
        Add(GlobalSettingKeys.TelecomRefundHighRiskMethods, telecom.Refund.HighRiskMethods ?? "");

        Add(GlobalSettingKeys.TelecomSuspensionBackOfficeTypes, telecom.Suspension.BackOfficeTypes ?? "");
        Add(GlobalSettingKeys.TelecomSuspensionCorporateRequiresBackOffice, telecom.Suspension.CorporateRequiresBackOffice.ToString().ToLowerInvariant());

        Add(GlobalSettingKeys.TelecomSimSwapLostStolenRequiresBackOffice, telecom.SimSwap.LostStolenRequiresBackOffice.ToString().ToLowerInvariant());

        Add(GlobalSettingKeys.TelecomChangeNumberPremiumCategories, telecom.ChangeNumber.PremiumCategories ?? "");
        Add(GlobalSettingKeys.TelecomChangeNumberBlockQuarantinedTarget, telecom.ChangeNumber.BlockQuarantinedTarget.ToString().ToLowerInvariant());

        Add(GlobalSettingKeys.TelecomTakeOverAlwaysRequiresBackOffice, telecom.TakeOver.AlwaysRequiresBackOffice.ToString().ToLowerInvariant());
        Add(GlobalSettingKeys.TelecomTakeOverDefaultDepositTransferPolicy, telecom.TakeOver.DefaultDepositTransferPolicy ?? "");

        Add(GlobalSettingKeys.TelecomDeviceSalesCreditScoreLowThreshold, telecom.DeviceSales.CreditScoreLowThreshold.ToString());
        Add(GlobalSettingKeys.TelecomDeviceSalesCreditScoreGoodThreshold, telecom.DeviceSales.CreditScoreGoodThreshold.ToString());
        Add(GlobalSettingKeys.TelecomDeviceSalesDownPaymentPercentReject, telecom.DeviceSales.DownPaymentPercentReject.ToString(System.Globalization.CultureInfo.InvariantCulture));
        Add(GlobalSettingKeys.TelecomDeviceSalesDownPaymentPercentVip, telecom.DeviceSales.DownPaymentPercentVip.ToString(System.Globalization.CultureInfo.InvariantCulture));
        Add(GlobalSettingKeys.TelecomDeviceSalesDownPaymentPercentLow, telecom.DeviceSales.DownPaymentPercentLow.ToString(System.Globalization.CultureInfo.InvariantCulture));
        Add(GlobalSettingKeys.TelecomDeviceSalesDownPaymentPercentGood, telecom.DeviceSales.DownPaymentPercentGood.ToString(System.Globalization.CultureInfo.InvariantCulture));

        Add(GlobalSettingKeys.TelecomPostpaidBadDebtThresholdMonths, telecom.BadDebt.PostpaidBadDebtThresholdMonths.ToString());

        // Legacy alias kept in sync for existing consumers.
        Add(GlobalSettingKeys.TelecomMaxActiveLinesPerIndividual, telecom.Activation.MaxLinesIndividual.ToString());

        return entries;
    }

    public static TelecomBusinessRulesDto CreateWithCatalogDefaults() => new();

    public static string GetCatalogDefault(string key) =>
        TelecomBusinessRulesCatalog.FindByKey(key)?.DefaultValue ?? string.Empty;

    private static bool ReadBool(IReadOnlyDictionary<string, string> map, string key, bool fallback) =>
        map.TryGetValue(key, out var raw) && bool.TryParse(raw, out var parsed) ? parsed : fallback;

    private static int ReadInt(IReadOnlyDictionary<string, string> map, string key, int fallback)
    {
        if (!map.TryGetValue(key, out var raw) || !int.TryParse(raw, out var parsed))
        {
            return fallback;
        }

        var def = TelecomBusinessRulesCatalog.FindByKey(key);
        if (def?.Min is decimal min && parsed < min)
        {
            return (int)min;
        }

        if (def?.Max is decimal max && parsed > max)
        {
            return (int)max;
        }

        return parsed;
    }

    private static decimal ReadDecimal(IReadOnlyDictionary<string, string> map, string key, decimal fallback)
    {
        if (!map.TryGetValue(key, out var raw) || !decimal.TryParse(raw, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var parsed))
        {
            return fallback;
        }

        var def = TelecomBusinessRulesCatalog.FindByKey(key);
        if (def?.Min is decimal min && parsed < min)
        {
            return min;
        }

        if (def?.Max is decimal max && parsed > max)
        {
            return max;
        }

        return parsed;
    }

    private static string ReadString(IReadOnlyDictionary<string, string> map, string key, string fallback) =>
        map.TryGetValue(key, out var raw) ? raw : fallback;
}
