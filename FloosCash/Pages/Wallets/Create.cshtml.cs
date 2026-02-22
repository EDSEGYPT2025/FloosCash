using FloosCash.Data;
using FloosCash.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FloosCash.Pages.Wallets
{
    public class CreateModel : PageModel
    {
        private readonly AppDbContext _context;

        public CreateModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Wallet Wallet { get; set; } = default!;

        public void OnGet()
        {
            // فتح الصفحة فارغة
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            _context.Wallets.Add(Wallet);
            await _context.SaveChangesAsync();

            // العودة إلى قائمة المحافظ بعد الحفظ بنجاح
            return RedirectToPage("./Index");
        }
    }
}