---
name: Payment Services Module
overview: تقييم وضع قسم 12 (Recharge, Voucher & Payment Services) في macquires-crm (~35% اليوم) وخطة Full-Stack لرفعه إلى ~85%+ عبر كيان معاملات دفع موحّد، توسيع بوابة الدفع/CBS، VAL-12، إشعارات، audit، KPIs — مع إعادة توجيه الشحن الحالي من Customer 360.
todos:
  - id: p1-fullstack-foundation
    content: "P1 Full-Stack: BE ledger/orchestrator/API + FE recharge modal 360/List (gateway ref, confirm, receipt)"
    status: pending
  - id: p2-fullstack-voucher
    content: "P2 Full-Stack: BE ValidateVoucher + SMS | FE voucher fields + Hub API migration"
    status: pending
  - id: p3-fullstack-reversal
    content: "P3 Full-Stack: BE reverse/audit/fraud | FE Finance reverse UI + fallout"
    status: pending
  - id: p4-fullstack-kpis
    content: "P4 Full-Stack: BE KPIs/tests | FE BackOffice card + E2E acceptance"
    status: pending
isProject: false
---

# خطة بناء Payment Services (ص 12 + ص 70) — Full-Stack

راجع الملف الكامل في [.cursor/plans/payment_services_module_5b8cc06f.plan.md](.cursor/plans/payment_services_module_5b8cc06f.plan.md) — نسخة العمل داخل المستودع.

**قاعدة:** Backend + Frontend + API في كل سبرنت.

**الوضع الحالي:** ~35% — شحن محلي في 360/List، CBS mock منفصل في Hub، لا voucher/reversal/KPIs دفع.

**الهدف:** ~85–88% بعد 4 سبرنتات.
