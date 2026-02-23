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

        // --- القائمة الجديدة لعرض كروت المحافظ ---
        public List<WalletSummaryCard> WalletSummaries { get; set; } = new List<WalletSummaryCard>();

        public async Task<IActionResult> OnGetAsync()
        {
            ActiveShift = await _context.Shifts
                .Include(s => s.AllowedWallets)
                .Include(s => s.Cashiers)
                .FirstOrDefaultAsync(s => !s.IsClosed);

            if (ActiveShift != null && User.IsInRole("Cashier"))
            {
                var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (int.TryParse(userIdString, out int userId))
                {
                    if (!ActiveShift.Cashiers.Any(c => c.Id == userId))
                    {
                        ActiveShift = null;
                    }
                }
            }

            if (ActiveShift != null)
            {
                var allowedWallets = ActiveShift.AllowedWallets.Where(w => w.IsActive).ToList();
                TotalWalletsBalance = allowedWallets.Sum(w => w.CurrentBalance);

                var today = DateTime.Today;
                var startOfMonth = new DateTime(today.Year, today.Month, 1);
                var allowedWalletIds = allowedWallets.Select(w => w.Id).ToList();

                // جلب عمليات الشهر الحالي لتوفير الأداء وحساب الليمت الشهري واليومي معاً
                var monthOperations = await _context.Operations
                    .Where(o => allowedWalletIds.Contains(o.WalletId) && o.Timestamp >= startOfMonth)
                    .ToListAsync();

                foreach (var wallet in allowedWallets)
                {
                    var walletMonthOps = monthOperations.Where(o => o.WalletId == wallet.Id).ToList();
                    var walletTodayOps = walletMonthOps.Where(o => o.Timestamp.Date == today).ToList();

                    var todayDeposit = walletTodayOps.Where(o => o.OperationType == "إيداع").Sum(o => o.Amount);
                    var todayWithdrawal = walletTodayOps.Where(o => o.OperationType == "سحب").Sum(o => o.Amount);

                    var monthDeposit = walletMonthOps.Where(o => o.OperationType == "إيداع").Sum(o => o.Amount);
                    var monthWithdrawal = walletMonthOps.Where(o => o.OperationType == "سحب").Sum(o => o.Amount);

                    // تعبئة بيانات الكارت لكل محفظة
                    WalletSummaries.Add(new WalletSummaryCard
                    {
                        Name = wallet.Name,
                        CurrentBalance = wallet.CurrentBalance,
                        RemainingDailyDeposit = wallet.DailyDepositLimit - todayDeposit,
                        RemainingDailyWithdrawal = wallet.DailyWithdrawalLimit - todayWithdrawal,
                        RemainingMonthlyDeposit = wallet.MonthlyDepositLimit - monthDeposit,
                        RemainingMonthlyWithdrawal = wallet.MonthlyWithdrawalLimit - monthWithdrawal,
                        DailyDepositLimit = wallet.DailyDepositLimit,
                        DailyWithdrawalLimit = wallet.DailyWithdrawalLimit
                    });

                    // التنبيهات (إذا تخطى 80% من الليمت اليومي)
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

                var shiftOperations = await _context.Operations
                    .Where(o => o.ShiftId == ActiveShift.Id)
                    .ToListAsync();

                TotalOperationsCount = shiftOperations.Count;
                TotalCommissions = shiftOperations.Sum(o => o.Commission);

                var totalDeposits = shiftOperations.Where(o => o.OperationType == "إيداع").Sum(o => o.Amount);
                var totalWithdrawals = shiftOperations.Where(o => o.OperationType == "سحب").Sum(o => o.Amount);

                ExpectedDrawerCash = ActiveShift.OpeningCash + totalDeposits - totalWithdrawals + TotalCommissions;

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

    // كلاس لتمرير بيانات كروت المحافظ للواجهة
    public class WalletSummaryCard
    {
        public string Name { get; set; } = string.Empty;
        public decimal CurrentBalance { get; set; }
        public decimal RemainingDailyDeposit { get; set; }
        public decimal RemainingDailyWithdrawal { get; set; }
        public decimal RemainingMonthlyDeposit { get; set; }
        public decimal RemainingMonthlyWithdrawal { get; set; }
        public decimal DailyDepositLimit { get; set; }
        public decimal DailyWithdrawalLimit { get; set; }
    }
}