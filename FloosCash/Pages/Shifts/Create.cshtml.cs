using FloosCash.Data;
using FloosCash.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FloosCash.Pages.Shifts
{
    [Authorize(Roles = "Admin")]
    public class CreateModel : PageModel
    {
        private readonly AppDbContext _context;

        public CreateModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Shift Shift { get; set; } = new Shift();

        [BindProperty]
        public List<int> SelectedWalletIds { get; set; } = new List<int>();

        // --- التعديل 1: إضافة مصفوفة لاستقبال الموظفين المحددين ---
        [BindProperty]
        public List<int> SelectedCashierIds { get; set; } = new List<int>();

        public List<Wallet> AvailableWallets { get; set; } = new List<Wallet>();

        // --- التعديل 2: قائمة لعرض الموظفين (الكاشيرية) في الشاشة ---
        public List<User> AvailableCashiers { get; set; } = new List<User>();

        public async Task<IActionResult> OnGetAsync()
        {
            AvailableWallets = await _context.Wallets.Where(w => w.IsActive).ToListAsync();
            // جلب الموظفين النشطين فقط
            AvailableCashiers = await _context.Users.Where(u => u.Role == "Cashier" && u.IsActive).ToListAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            AvailableWallets = await _context.Wallets.Where(w => w.IsActive).ToListAsync();
            AvailableCashiers = await _context.Users.Where(u => u.Role == "Cashier" && u.IsActive).ToListAsync();

            if (SelectedWalletIds == null || !SelectedWalletIds.Any())
            {
                ModelState.AddModelError("SelectedWalletIds", "عفواً، يجب تحديد محفظة واحدة على الأقل.");
                return Page();
            }

            // --- التعديل 3: التأكد من اختيار موظف واحد على الأقل ---
            if (SelectedCashierIds == null || !SelectedCashierIds.Any())
            {
                ModelState.AddModelError("SelectedCashierIds", "عفواً، يجب اختيار كاشير واحد على الأقل لهذه الوردية.");
                return Page();
            }

            var selectedWallets = await _context.Wallets.Where(w => SelectedWalletIds.Contains(w.Id)).ToListAsync();
            var selectedCashiers = await _context.Users.Where(u => SelectedCashierIds.Contains(u.Id)).ToListAsync();

            Shift.AllowedWallets = selectedWallets;
            Shift.Cashiers = selectedCashiers;

            // دمج أسماء الموظفين المختارين ليتم عرضها بسهولة في التقارير (مثال: أحمد - محمود)
            Shift.EmployeeName = string.Join(" - ", selectedCashiers.Select(c => c.FullName));

            Shift.StartTime = DateTime.Now;
            Shift.IsClosed = false;

            // إفراغ حقل الـ EmployeeName من الـ ModelState لأنه أصبح يحسب برمجياً ولم يعد إدخالاً من الشاشة
            ModelState.Remove("Shift.EmployeeName");

            if (!ModelState.IsValid) return Page();

            _context.Shifts.Add(Shift);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"تم فتح الوردية للموظفين ({Shift.EmployeeName}) بنجاح.";
            return RedirectToPage("/Index");
        }
    }
}