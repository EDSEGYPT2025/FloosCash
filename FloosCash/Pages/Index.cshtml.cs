using FloosCash.Data;
using FloosCash.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FloosCash.Pages
{
    [Authorize]
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

        public List<WalletAlert> Alerts { get; set; } = new List<WalletAlert>();

        public async Task<IActionResult> OnGetAsync()
        {
            // --- التعديل 1: جلب الوردية شاملة المحافظ والموظفين ---
            ActiveShift = await _context.Shifts
                .Include(s => s.AllowedWallets)
                .Include(s => s.Cashiers) // تم إضافة هذا السطر
                .FirstOrDefaultAsync(s => !s.IsClosed);

            // --- التعديل 2: حماية الوردية (هل الكاشير الحالي من ضمن موظفيها؟) ---
            if (ActiveShift != null && User.IsInRole("Cashier"))
            {
                var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (int.TryParse(userIdString, out int userId))
                {
                    // إذا لم يكن الموظف ضمن القائمة المحددة للوردية، قم بإخفائها عنه
                    if (!ActiveShift.Cashiers.Any(c => c.Id == userId))
                    {
                        ActiveShift = null;
                    }
                }
            }
            // ------------------------------------------------------------------

            if (ActiveShift != null)
            {
                // 1. حساب إجمالي أرصدة "المحافظ المسلمة للموظف فقط" وليس الشركة كلها
                var allowedWallets = ActiveShift.AllowedWallets.Where(w => w.IsActive).ToList();
                TotalWalletsBalance = allowedWallets.Sum(w => w.CurrentBalance);

                // 2. حساب تنبيهات المحافظ (الخاصة بالموظف فقط) لليوم الحالي
                var today = DateTime.Today;
                var todayOperations = await _context.Operations
                    .Where(o => o.Timestamp.Date == today)
                    .ToListAsync();

                foreach (var wallet in allowedWallets)
                {
                    var walletTodayOps = todayOperations.Where(o => o.WalletId == wallet.Id).ToList();
                    var todayDeposit = walletTodayOps.Where(o => o.OperationType == "إيداع").Sum(o => o.Amount);
                    var todayWithdrawal = walletTodayOps.Where(o => o.OperationType == "سحب").Sum(o => o.Amount);

                    if (wallet.DailyDepositLimit > 0 && (todayDeposit / wallet.DailyDepositLimit) >= 0.8m)
                    {
                        Alerts.Add(new WalletAlert
                        {
                            WalletName = wallet.Name,
                            Message = $"اقتربت من الحد اليومي للإيداع ({(todayDeposit / wallet.DailyDepositLimit) * 100:0.##}%) - المتبقي {wallet.DailyDepositLimit - todayDeposit} ج",
                            Type = "warning"
                        });
                    }

                    if (wallet.DailyWithdrawalLimit > 0 && (todayWithdrawal / wallet.DailyWithdrawalLimit) >= 0.8m)
                    {
                        Alerts.Add(new WalletAlert
                        {
                            WalletName = wallet.Name,
                            Message = $"اقتربت من الحد اليومي للسحب ({(todayWithdrawal / wallet.DailyWithdrawalLimit) * 100:0.##}%) - المتبقي {wallet.DailyWithdrawalLimit - todayWithdrawal} ج",
                            Type = "danger"
                        });
                    }
                }

                // 3. جلب كل عمليات الوردية الحالية
                var shiftOperations = await _context.Operations
                    .Where(o => o.ShiftId == ActiveShift.Id)
                    .ToListAsync();

                TotalOperationsCount = shiftOperations.Count;
                TotalCommissions = shiftOperations.Sum(o => o.Commission);

                // 4. الحسبة المحاسبية للنقدية المتوقعة في الدرج
                var totalDeposits = shiftOperations.Where(o => o.OperationType == "إيداع").Sum(o => o.Amount);
                var totalWithdrawals = shiftOperations.Where(o => o.OperationType == "سحب").Sum(o => o.Amount);

                ExpectedDrawerCash = ActiveShift.OpeningCash + totalDeposits - totalWithdrawals + TotalCommissions;

                // 5. جلب آخر 5 عمليات
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

    public class WalletAlert
    {
        public string WalletName { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = "warning";
    }
}