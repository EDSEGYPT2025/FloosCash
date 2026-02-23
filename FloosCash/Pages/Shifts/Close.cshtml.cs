using FloosCash.Data;
using FloosCash.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FloosCash.Pages.Shifts
{
    [Authorize] // السماح بالدخول للكاشير (أو المدير للمراقبة)
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
        public decimal ActualCash { get; set; }

        // --- التعديل: قائمة لعرض كروت المحافظ في التقفيل ---
        public List<WalletSummaryCard> WalletSummaries { get; set; } = new List<WalletSummaryCard>();

        public async Task<IActionResult> OnGetAsync()
        {
            // جلب الوردية مع المحافظ المسموحة والموظفين
            ActiveShift = await _context.Shifts
                .Include(s => s.AllowedWallets)
                .Include(s => s.Cashiers)
                .FirstOrDefaultAsync(s => !s.IsClosed);

            if (ActiveShift == null)
            {
                return RedirectToPage("/Index");
            }

            // حماية: التأكد أن الكاشير الحالي من ضمن موظفي هذه الوردية
            if (User.IsInRole("Cashier"))
            {
                var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (int.TryParse(userIdString, out int userId))
                {
                    if (!ActiveShift.Cashiers.Any(c => c.Id == userId))
                    {
                        return RedirectToPage("/Index");
                    }
                }
            }

            await CalculateShiftSummary();
            await LoadWalletsSummary();

            // وضع النقدية المتوقعة كقيمة افتراضية في حقل الإدخال للتسهيل
            ActualCash = ExpectedCash;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            ActiveShift = await _context.Shifts
                .Include(s => s.Cashiers)
                .FirstOrDefaultAsync(s => !s.IsClosed);

            if (ActiveShift == null) return RedirectToPage("/Index");

            if (User.IsInRole("Cashier"))
            {
                var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (int.TryParse(userIdString, out int userId))
                {
                    if (!ActiveShift.Cashiers.Any(c => c.Id == userId))
                    {
                        return RedirectToPage("/Index");
                    }
                }
            }

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

        // --- التعديل: دالة لحساب أرصدة وليمت المحافظ ---
        private async Task LoadWalletsSummary()
        {
            var allowedWallets = ActiveShift.AllowedWallets.Where(w => w.IsActive).ToList();
            if (!allowedWallets.Any()) return;

            var today = DateTime.Today;
            var startOfMonth = new DateTime(today.Year, today.Month, 1);
            var allowedWalletIds = allowedWallets.Select(w => w.Id).ToList();

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
            }
        }
    }

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