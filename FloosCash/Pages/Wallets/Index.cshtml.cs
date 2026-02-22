using FloosCash.Data;
using FloosCash.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FloosCash.Pages.Wallets
{
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;

        public IndexModel(AppDbContext context)
        {
            _context = context;
        }

        public IList<Wallet> Wallets { get; set; } = default!;

        public async Task OnGetAsync()
        {
            Wallets = await _context.Wallets.ToListAsync();
        }
    }
}