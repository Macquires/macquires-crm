# مصفوفة ملكية القدرة — بيع خط / تفعيل جديد (صفحة 5)

| المجال | بيع خط، تغيير GSM، حظر، إعادة اتصال، إنهاء |
|--------|---------------------------------------------|
| **القدرة** | Selling Line & New Activation |
| **الكود** | `TelecomOperationKind.NewActivation` (ACT-) |

## المالكون (Capability Owners)

| الدور | Identity Role | صلاحية | مسؤولية |
|-------|---------------|---------|---------|
| موظف فرع (Branch CSR) | `TelecomShowroom` | `telecom.line.activate` | بدء الطلب، التحقق، التنفيذ |
| وكيل موزع (Dealer) | `TelecomShowroom` + `ActivationChannel=Dealer` | `telecom.line.activate` | نفس المسار مع `DealerCode` |
| مسؤول تفعيل | `TelecomBackOffice` | `telecom.line.activate` | استثناءات / override |
| الفوترة | `HuaweiCbsBillingIntegration` | — | حساب CBS / وديعة |
| التزويد | `HlrNetworkProvisioningService` | — | إنشاء مشترك HLR |

مرجع الكود: `Core/Application/Common/Telecom/SellingLine/SellingLineCapabilityOwnership.cs`
