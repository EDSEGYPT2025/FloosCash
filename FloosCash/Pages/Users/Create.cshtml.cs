using FloosCash.Data;
using FloosCash.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FloosCash.Pages.Users
{
    [Authorize(Roles = "Admin")]
    public class CreateModel : PageModel
    {
        private readonly AppDbContext _context;
        public CreateModel(AppDbContext context) => _context = context;

        [BindProperty]
        public User NewUser { get; set; } = new User();

        public IActionResult OnGet() => Page();

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            // التأكد من عدم تكرار اسم المستخدم
            if (_context.Users.Any(u => u.Username == NewUser.Username))
            {
                ModelState.AddModelError("NewUser.Username", "اسم المستخدم هذا مسجل مسبقاً، اختر اسماً آخر.");
                return Page();
            }

            _context.Users.Add(NewUser);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "تم إضافة المستخدم بنجاح.";
            return RedirectToPage("./Index");
        }
    }
}