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
    [Authorize]
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

        public async Task<IActionResult> OnGetAsync()
        {
            // --- التعديل: جلب الوردية مع المحافظ والموظفين ---
            ActiveShift = await _context.Shifts
                .Include(s => s.AllowedWallets)
                .Include(s => s.Cashiers)
                .FirstOrDefaultAsync(s => !s.IsClosed);

            // --- حماية إضافية للكاشير غير المسموح له ---
            if (ActiveShift != null && User.IsInRole("Cashier"))
            {
                var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (int.TryParse(userIdString, out int userId))
                {
                    if (!ActiveShift.Cashiers.Any(c => c.Id == userId))
                    {
                        return RedirectToPage("/Index"); // طرد الكاشير من الصفحة
                    }
                }
            }

            if (ActiveShift == null)
            {
                return RedirectToPage("/Shifts/Create");
            }

            // عرض المحافظ الخاصة بهذه الوردية فقط
            var allowedWallets = ActiveShift.AllowedWallets.Where(w => w.IsActive).ToList();
            WalletsList = new SelectList(allowedWallets, "Id", "Name");

            WalletsJson = JsonSerializer.Serialize(allowedWallets.Select(w => new {
                id = w.Id,
                depositRate = w.DepositCommissionRate,
                withdrawalRate = w.WithdrawalCommissionRate
            }));

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            ActiveShift = await _context.Shifts
                .Include(s => s.AllowedWallets)
                .Include(s => s.Cashiers)
                .FirstOrDefaultAsync(s => !s.IsClosed);

            // التأكد مرة أخرى عند الحفظ
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

            var wallet = await _context.Wallets.FindAsync(Operation.WalletId);

            // حماية إضافية: التأكد أن المحفظة المرسلة من الواجهة مسموح بها في هذه الوردية
            if (wallet == null || !ActiveShift.AllowedWallets.Any(w => w.Id == wallet.Id))
            {
                ModelState.AddModelError(string.Empty, "عفواً، غير مصرح لك باستخدام هذه المحفظة في ورديتك الحالية.");
                await LoadFormData();
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
                if (todayTotal + Operation.Amount > wallet.DailyDepositLimit)
                {
                    ModelState.AddModelError(string.Empty, $"عفواً، هذه العملية تتخطى حد الإيداع اليومي للمحفظة ({wallet.DailyDepositLimit} ج.م). المتبقي لليوم هو {wallet.DailyDepositLimit - todayTotal} ج.م.");
                    await LoadFormData();
                    return Page();
                }
                if (monthTotal + Operation.Amount > wallet.MonthlyDepositLimit)
                {
                    ModelState.AddModelError(string.Empty, $"عفواً، هذه العملية تتخطى حد الإيداع الشهري للمحفظة ({wallet.MonthlyDepositLimit} ج.م). المتبقي للشهر هو {wallet.MonthlyDepositLimit - monthTotal} ج.م.");
                    await LoadFormData();
                    return Page();
                }

                if (Operation.Commission == 0 && wallet.DepositCommissionRate > 0)
                {
                    Operation.Commission = Operation.Amount * (wallet.DepositCommissionRate / 100);
                }
            }
            else if (Operation.OperationType == "سحب")
            {
                if (todayTotal + Operation.Amount > wallet.DailyWithdrawalLimit)
                {
                    ModelState.AddModelError(string.Empty, $"عفواً، هذه العملية تتخطى حد السحب اليومي للمحفظة ({wallet.DailyWithdrawalLimit} ج.م). المتبقي لليوم هو {wallet.DailyWithdrawalLimit - todayTotal} ج.م.");
                    await LoadFormData();
                    return Page();
                }
                if (monthTotal + Operation.Amount > wallet.MonthlyWithdrawalLimit)
                {
                    ModelState.AddModelError(string.Empty, $"عفواً، هذه العملية تتخطى حد السحب الشهري للمحفظة ({wallet.MonthlyWithdrawalLimit} ج.م). المتبقي للشهر هو {wallet.MonthlyWithdrawalLimit - monthTotal} ج.م.");
                    await LoadFormData();
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
                    wallet.CurrentBalance -= Operation.Amount;
                }
                else if (Operation.OperationType == "سحب")
                {
                    wallet.CurrentBalance += Operation.Amount;
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
                await LoadFormData();
                return Page();
            }
        }

        private async Task LoadFormData()
        {
            // إعادة تحميل الوردية ومحافظها
            ActiveShift = await _context.Shifts.Include(s => s.AllowedWallets).FirstOrDefaultAsync(s => !s.IsClosed);
            var allowedWallets = ActiveShift?.AllowedWallets.Where(w => w.IsActive).ToList() ?? new List<Wallet>();

            WalletsList = new SelectList(allowedWallets, "Id", "Name");

            WalletsJson = JsonSerializer.Serialize(allowedWallets.Select(w => new {
                id = w.Id,
                depositRate = w.DepositCommissionRate,
                withdrawalRate = w.WithdrawalCommissionRate
            }));
        }
    }
}