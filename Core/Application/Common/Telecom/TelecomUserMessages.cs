using Application.Common;

namespace Application.Common.Telecom;

public static class TelecomUserMessages
{
    public static readonly BilingualUserMessage ValAct12KycRequired = new(
        Ar: "VAL-ACT-12: يجب رفع وثيقة KYC وربطها بالطلب قبل تأكيد التفعيل.",
        En: "VAL-ACT-12: A KYC document must be uploaded and linked before activation confirmation.",
        Code: "VAL-ACT-12");

    public static readonly BilingualUserMessage ValAct12KycVaultMissing = new(
        Ar: "VAL-ACT-12: وثيقة KYC غير موجودة في الخزنة الآمنة. يرجى رفع الهوية مرة أخرى.",
        En: "VAL-ACT-12: KYC document was not found in the secure vault. Please upload the ID again.",
        Code: "VAL-ACT-12");

    public static readonly BilingualUserMessage KycFileRequired = new(
        Ar: "ملف وثيقة KYC مطلوب.",
        En: "KYC document file is required.");

    public static readonly BilingualUserMessage KycFileTooLarge = new(
        Ar: "حجم الملف يتجاوز 5 ميجابايت.",
        En: "File size exceeds 5 MB.");

    public static readonly BilingualUserMessage KycFileTypeInvalid = new(
        Ar: "يُسمح فقط بملفات PDF أو صور (JPG/PNG).",
        En: "Only PDF or image files (JPG/PNG) are allowed.");

    public static readonly BilingualUserMessage KycMsisdnRequired = new(
        Ar: "رقم MSISDN مطلوب لربط وثيقة KYC.",
        En: "MSISDN is required to link the KYC document.");
}
