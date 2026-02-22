using FloosCash.Data;
using FloosCash.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FloosCash.Pages.Operations
{
    public class CreateModel : PageModel
    {
        private readonly AppDbContext _context;

        public CreateModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Operation Operation { get; set; } = new Operation();

        // قائمة المحافظ لعرضها في الشاشة
        public SelectList WalletsList { get; set; } = default!;

        // الوردية المفتوحة حالياً
        public Shift ActiveShift { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync()
        {
            // 1. التأكد من وجود وردية مفتوحة أولاً
            ActiveShift = await _context.Shifts.FirstOrDefaultAsync(s => !s.IsClosed);
            if (ActiveShift == null)
            {
                // إذا لم تكن هناك وردية، نمنعه من تسجيل العمليات ونوجهه لفتح وردية
                return RedirectToPage("/Shifts/Create");
            }

            // 2. جلب المحافظ النشطة لعرضها في القائمة المنسدلة
            var wallets = await _context.Wallets.Where(w => w.IsActive).ToListAsync();
            WalletsList = new SelectList(wallets, "Id", "Name");

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            ActiveShift = await _context.Shifts.FirstOrDefaultAsync(s => !s.IsClosed);
            if (ActiveShift == null) return RedirectToPage("/Shifts/Create");

            var wallet = await _context.Wallets.FindAsync(Operation.WalletId);
            if (wallet == null) return Page();

            // ضبط بيانات العملية
            Operation.ShiftId = ActiveShift.Id;
            Operation.Timestamp = DateTime.Now;
            Operation.Notes ??= "";

            // --- بداية المعاملة المالية الآمنة (Transaction) ---
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (Operation.OperationType == "إيداع")
                {
                    // العميل يدفع كاش ويأخذ رصيد
                    // 1. خصم المبلغ من المحفظة الإلكترونية للشركة
                    wallet.CurrentBalance -= Operation.Amount;
                    // (ملاحظة: النقدية في الدرج سنحسبها في شاشة التسوية بجمع الإيداعات)
                }
                else if (Operation.OperationType == "سحب")
                {
                    // العميل يحول رصيد ويأخذ كاش
                    // 1. إضافة المبلغ للمحفظة الإلكترونية للشركة
                    wallet.CurrentBalance += Operation.Amount;
                }

                // 2. حفظ العملية
                _context.Operations.Add(Operation);

                // 3. تحديث المحفظة
                _context.Wallets.Update(wallet);

                // 4. تنفيذ الحفظ الفعلي في قاعدة البيانات
                await _context.SaveChangesAsync();

                // 5. اعتماد المعاملة
                await transaction.CommitAsync();

                // --- أضف هذا السطر لإرسال رسالة النجاح للواجهة الأمامية ---
                TempData["SuccessMessage"] = $"تم تسجيل عملية {Operation.OperationType} بمبلغ {Operation.Amount} ج.م بنجاح.";

                // العودة لنفس الصفحة لتسجيل عملية أخرى بسرعة
                return RedirectToPage("./Create");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError(string.Empty, "حدث خطأ أثناء حفظ العملية: " + ex.Message);

                // إعادة تحميل القائمة في حالة الخطأ
                WalletsList = new SelectList(await _context.Wallets.Where(w => w.IsActive).ToListAsync(), "Id", "Name");
                return Page();
            }
        }
    }
}