using FloosCash.Data;
using FloosCash.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace FloosCash.Pages.Operations
{
    [Authorize(Roles = "Cashier")]
    public class CreateModel : PageModel
    {
        private readonly AppDbContext _context;

        public CreateModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Operation Operation { get; set; } = new Operation();

        public SelectList WalletsList { get; set; } = default!;
        public Shift ActiveShift { get; set; } = default!;

        public string WalletsJson { get; set; } = "[]";

        // --- التعديل: خاصية رصيد الدرج ---
        public decimal ExpectedDrawerCash { get; set; }

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
                        return RedirectToPage("/Index");
                    }
                }
            }

            if (ActiveShift == null)
            {
                return RedirectToPage("/Shifts/Create");
            }

            await LoadFormData();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
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
                        return RedirectToPage("/Index");
                    }
                }
            }

            if (ActiveShift == null) return RedirectToPage("/Shifts/Create");

            // نحمل البيانات أولاً لكي تكون جاهزة (مثل رصيد الدرج) لو حدث خطأ وأعدنا عرض الصفحة
            await LoadFormData();

            var wallet = await _context.Wallets.FindAsync(Operation.WalletId);

            if (wallet == null || !ActiveShift.AllowedWallets.Any(w => w.Id == wallet.Id))
            {
                ModelState.AddModelError(string.Empty, "عفواً، غير مصرح لك باستخدام هذه المحفظة في ورديتك الحالية.");
                return Page();
            }

            var today = DateTime.Today;
            var startOfMonth = new DateTime(today.Year, today.Month, 1);

            var todayTotal = await _context.Operations
                .Where(o => o.WalletId == wallet.Id && o.OperationType == Operation.OperationType && o.Timestamp.Date == today)
                .SumAsync(o => o.Amount);

            var monthTotal = await _context.Operations
                .Where(o => o.WalletId == wallet.Id && o.OperationType == Operation.OperationType && o.Timestamp >= startOfMonth)
                .SumAsync(o => o.Amount);

            if (Operation.OperationType == "إيداع")
            {
                // --- التعديل: فحص رصيد المحفظة للإيداع ---
                if (Operation.Amount > wallet.CurrentBalance)
                {
                    ModelState.AddModelError(string.Empty, $"عفواً، الرصيد الفعلي في المحفظة ({wallet.CurrentBalance} ج.م) لا يكفي لتحويل هذا المبلغ للعميل.");
                    return Page();
                }

                if (todayTotal + Operation.Amount > wallet.DailyDepositLimit)
                {
                    ModelState.AddModelError(string.Empty, $"عفواً، هذه العملية تتخطى حد الإيداع اليومي للمحفظة ({wallet.DailyDepositLimit} ج.م). المتبقي لليوم هو {wallet.DailyDepositLimit - todayTotal} ج.م.");
                    return Page();
                }
                if (monthTotal + Operation.Amount > wallet.MonthlyDepositLimit)
                {
                    ModelState.AddModelError(string.Empty, $"عفواً، هذه العملية تتخطى حد الإيداع الشهري للمحفظة ({wallet.MonthlyDepositLimit} ج.م). المتبقي للشهر هو {wallet.MonthlyDepositLimit - monthTotal} ج.م.");
                    return Page();
                }

                if (Operation.Commission == 0 && wallet.DepositCommissionRate > 0)
                {
                    Operation.Commission = Operation.Amount * (wallet.DepositCommissionRate / 100);
                }
            }
            else if (Operation.OperationType == "سحب")
            {
                // --- التعديل: فحص رصيد الدرج للسحب ---
                if (Operation.Amount > ExpectedDrawerCash)
                {
                    ModelState.AddModelError(string.Empty, $"عفواً، النقدية المتاحة في الدرج ({ExpectedDrawerCash} ج.م) لا تكفي لتسليم هذا المبلغ للعميل.");
                    return Page();
                }

                if (todayTotal + Operation.Amount > wallet.DailyWithdrawalLimit)
                {
                    ModelState.AddModelError(string.Empty, $"عفواً، هذه العملية تتخطى حد السحب اليومي للمحفظة ({wallet.DailyWithdrawalLimit} ج.م). المتبقي لليوم هو {wallet.DailyWithdrawalLimit - todayTotal} ج.م.");
                    return Page();
                }
                if (monthTotal + Operation.Amount > wallet.MonthlyWithdrawalLimit)
                {
                    ModelState.AddModelError(string.Empty, $"عفواً، هذه العملية تتخطى حد السحب الشهري للمحفظة ({wallet.MonthlyWithdrawalLimit} ج.م). المتبقي للشهر هو {wallet.MonthlyWithdrawalLimit - monthTotal} ج.م.");
                    return Page();
                }

                if (Operation.Commission == 0 && wallet.WithdrawalCommissionRate > 0)
                {
                    Operation.Commission = Operation.Amount * (wallet.WithdrawalCommissionRate / 100);
                }
            }

            Operation.ShiftId = ActiveShift.Id;
            Operation.Timestamp = DateTime.Now;
            Operation.Notes ??= "";

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (Operation.OperationType == "إيداع")
                {
                    wallet.CurrentBalance -= Operation.Amount; // المحفظة تقل
                }
                else if (Operation.OperationType == "سحب")
                {
                    wallet.CurrentBalance += Operation.Amount; // المحفظة تزيد
                }

                _context.Operations.Add(Operation);
                _context.Wallets.Update(wallet);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["SuccessMessage"] = $"تم تسجيل عملية {Operation.OperationType} بمبلغ {Operation.Amount} ج.م بنجاح. (العمولة: {Operation.Commission})";
                return RedirectToPage("./Create");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError(string.Empty, "حدث خطأ أثناء حفظ العملية: " + ex.Message);
                return Page();
            }
        }

        private async Task LoadFormData()
        {
            if (ActiveShift == null) return;

            var allowedWallets = ActiveShift.AllowedWallets.Where(w => w.IsActive).ToList();
            WalletsList = new SelectList(allowedWallets, "Id", "Name");

            var today = DateTime.Today;
            var startOfMonth = new DateTime(today.Year, today.Month, 1);
            var allowedWalletIds = allowedWallets.Select(w => w.Id).ToList();

            var shiftOperations = await _context.Operations
                .Where(o => o.ShiftId == ActiveShift.Id)
                .ToListAsync();

            // --- التعديل: حساب رصيد الدرج الفعلي ---
            var totalDeposits = shiftOperations.Where(o => o.OperationType == "إيداع").Sum(o => o.Amount);
            var totalWithdrawals = shiftOperations.Where(o => o.OperationType == "سحب").Sum(o => o.Amount);
            var totalCommissions = shiftOperations.Sum(o => o.Commission);

            ExpectedDrawerCash = ActiveShift.OpeningCash + totalDeposits - totalWithdrawals + totalCommissions;
            // ----------------------------------------

            var monthOperations = await _context.Operations
                .Where(o => allowedWalletIds.Contains(o.WalletId) && o.Timestamp >= startOfMonth)
                .ToListAsync();

            var walletsData = allowedWallets.Select(w => {
                var wMonthOps = monthOperations.Where(o => o.WalletId == w.Id).ToList();
                var wTodayOps = wMonthOps.Where(o => o.Timestamp.Date == today).ToList();

                var tDep = wTodayOps.Where(o => o.OperationType == "إيداع").Sum(o => o.Amount);
                var tWdw = wTodayOps.Where(o => o.OperationType == "سحب").Sum(o => o.Amount);
                var mDep = wMonthOps.Where(o => o.OperationType == "إيداع").Sum(o => o.Amount);
                var mWdw = wMonthOps.Where(o => o.OperationType == "سحب").Sum(o => o.Amount);

                return new
                {
                    id = w.Id,
                    currentBalance = w.CurrentBalance,
                    depositRate = w.DepositCommissionRate,
                    withdrawalRate = w.WithdrawalCommissionRate,
                    remainingDailyDeposit = w.DailyDepositLimit - tDep,
                    remainingDailyWithdrawal = w.DailyWithdrawalLimit - tWdw,
                    remainingMonthlyDeposit = w.MonthlyDepositLimit - mDep,
                    remainingMonthlyWithdrawal = w.MonthlyWithdrawalLimit - mWdw
                };
            });

            WalletsJson = JsonSerializer.Serialize(walletsData);
        }
    }
}