using FloosCash.Data;
using FloosCash.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FloosCash.Pages.Users
{
    [Authorize(Roles = "Admin")] // حماية الصفحة للمدير فقط
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;
        public IndexModel(AppDbContext context) => _context = context;

        public IList<User> UsersList { get; set; } = default!;

        public async Task OnGetAsync()
        {
            UsersList = await _context.Users.ToListAsync();
        }
    }
}