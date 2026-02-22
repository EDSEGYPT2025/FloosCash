using FloosCash.Data;
using FloosCash.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FloosCash.Pages
{
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;

        public IndexModel(AppDbContext context)
        {
            _context = context;
        }

        public Shift? ActiveShift { get; set; }
        public decimal ExpectedDrawerCash { get; set; }
        public decimal TotalWalletsBalance { get; set; }
        public decimal TotalCommissions { get; set; }
        public int TotalOperationsCount { get; set; }

        public IList<Operation> RecentOperations { get; set; } = new List<Operation>();

        public async Task<IActionResult> OnGetAsync()
        {
            // 1. جلب الوردية المفتوحة حالياً
            ActiveShift = await _context.Shifts.FirstOrDefaultAsync(s => !s.IsClosed);

            // 2. حساب إجمالي أرصدة المحافظ الإلكترونية كلها (لمعرفة سيولة الشركة)
            TotalWalletsBalance = await _context.Wallets
                .Where(w => w.IsActive)
                .SumAsync(w => w.CurrentBalance);

            if (ActiveShift != null)
            {
                // 3. جلب كل عمليات الوردية الحالية
                var shiftOperations = await _context.Operations
                    .Where(o => o.ShiftId == ActiveShift.Id)
                    .ToListAsync();

                TotalOperationsCount = shiftOperations.Count;
                TotalCommissions = shiftOperations.Sum(o => o.Commission);

                // 4. الحسبة المحاسبية للنقدية المتوقعة في الدرج
                // = الرصيد الافتتاحي + الإيداعات (الكاش المستلم) - السحوبات (الكاش المدفوع) + العمولات (الكاش المستلم كأرباح)
                var totalDeposits = shiftOperations.Where(o => o.OperationType == "إيداع").Sum(o => o.Amount);
                var totalWithdrawals = shiftOperations.Where(o => o.OperationType == "سحب").Sum(o => o.Amount);

                ExpectedDrawerCash = ActiveShift.OpeningCash + totalDeposits - totalWithdrawals + TotalCommissions;

                // 5. جلب آخر 5 عمليات لعرضها في الجدول السريع
                RecentOperations = await _context.Operations
                    .Include(o => o.Wallet)
                    .Where(o => o.ShiftId == ActiveShift.Id)
                    .OrderByDescending(o => o.Timestamp)
                    .Take(5)
                    .ToListAsync();
            }

            return Page();
        }
    }
}