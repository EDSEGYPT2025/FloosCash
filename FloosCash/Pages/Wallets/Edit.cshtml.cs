using FloosCash.Data;
using FloosCash.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FloosCash.Pages.Wallets
{
    public class EditModel : PageModel
    {
        private readonly AppDbContext _context;

        public EditModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Wallet Wallet { get; set; } = default!;

        // جلب بيانات المحفظة عند فتح الصفحة
        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var wallet = await _context.Wallets.FirstOrDefaultAsync(m => m.Id == id);

            if (wallet == null)
            {
                return NotFound();
            }

            Wallet = wallet;
            return Page();
        }

        // حفظ التعديلات
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // إخبار Entity Framework بأن هذه البيانات تم تعديلها ويجب تحديثها
            _context.Attach(Wallet).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!WalletExists(Wallet.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            // العودة لقائمة المحافظ بعد نجاح التعديل
            return RedirectToPage("./Index");
        }

        private bool WalletExists(int id)
        {
            return _context.Wallets.Any(e => e.Id == id);
        }
    }
}