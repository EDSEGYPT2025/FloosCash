using FloosCash.Data;
using FloosCash.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FloosCash.Pages.Shifts
{
    public class CloseModel : PageModel
    {
        private readonly AppDbContext _context;

        public CloseModel(AppDbContext context)
        {
            _context = context;
        }

        public Shift ActiveShift { get; set; } = default!;

        // متغيرات لعرض الملخص في الشاشة
        public decimal ExpectedCash { get; set; }
        public int OperationsCount { get; set; }
        public decimal TotalDeposits { get; set; }
        public decimal TotalWithdrawals { get; set; }
        public decimal TotalCommissions { get; set; }

        [BindProperty]
        public decimal ActualCash { get; set; } // النقدية الفعلية التي سيُدخلها الكاشير

        public async Task<IActionResult> OnGetAsync()
        {
            ActiveShift = await _context.Shifts.FirstOrDefaultAsync(s => !s.IsClosed);

            if (ActiveShift == null)
            {
                return RedirectToPage("/Index"); // لا توجد وردية مفتوحة
            }

            await CalculateShiftSummary();

            // وضع النقدية المتوقعة كقيمة افتراضية في حقل الإدخال للتسهيل
            ActualCash = ExpectedCash;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            ActiveShift = await _context.Shifts.FirstOrDefaultAsync(s => !s.IsClosed);

            if (ActiveShift == null) return RedirectToPage("/Index");

            // تحديث بيانات الوردية وإغلاقها
            ActiveShift.ClosingCash = ActualCash;
            ActiveShift.EndTime = DateTime.Now;
            ActiveShift.IsClosed = true;

            _context.Shifts.Update(ActiveShift);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"تم إغلاق وردية الموظف {ActiveShift.EmployeeName} وتسوية العهدة بنجاح!";
            return RedirectToPage("/Index");
        }

        private async Task CalculateShiftSummary()
        {
            var ops = await _context.Operations.Where(o => o.ShiftId == ActiveShift.Id).ToListAsync();

            OperationsCount = ops.Count;
            TotalDeposits = ops.Where(o => o.OperationType == "إيداع").Sum(o => o.Amount);
            TotalWithdrawals = ops.Where(o => o.OperationType == "سحب").Sum(o => o.Amount);
            TotalCommissions = ops.Sum(o => o.Commission);

            // الحسبة المحاسبية للدرج
            ExpectedCash = ActiveShift.OpeningCash + TotalDeposits - TotalWithdrawals + TotalCommissions;
        }
    }
}