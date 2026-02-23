using FloosCash.Data;
using FloosCash.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FloosCash.Pages.Reports
{
    [Authorize(Roles = "Admin")] // حماية للمدير فقط
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;

        public IndexModel(AppDbContext context)
        {
            _context = context;
        }

        // متغيرات الفلترة (بحث بالتاريخ والمحفظة)
        [BindProperty(SupportsGet = true)]
        public DateTime? StartDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? EndDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? WalletId { get; set; }

        public SelectList WalletsList { get; set; } = default!;

        // نتائج التقرير
        public List<Operation> ReportOperations { get; set; } = new List<Operation>();
        public decimal TotalDeposits { get; set; }
        public decimal TotalWithdrawals { get; set; }
        public decimal TotalCommissions { get; set; }

        public async Task OnGetAsync()
        {
            // جلب المحافظ لملء القائمة المنسدلة
            var wallets = await _context.Wallets.ToListAsync();
            WalletsList = new SelectList(wallets, "Id", "Name");

            // بناء الاستعلام (Query) لجلب العمليات
            var query = _context.Operations
                .Include(o => o.Wallet)
                .Include(o => o.Shift)
                    .ThenInclude(s => s.Cashiers) // لجلب أسماء الموظفين
                .AsQueryable();

            // تطبيق الفلاتر إذا أدخلها المدير
            if (StartDate.HasValue)
            {
                query = query.Where(o => o.Timestamp.Date >= StartDate.Value.Date);
            }
            else
            {
                // الافتراضي: عرض بيانات اليوم الحالي
                StartDate = DateTime.Today;
                query = query.Where(o => o.Timestamp.Date >= StartDate.Value.Date);
            }

            if (EndDate.HasValue)
            {
                query = query.Where(o => o.Timestamp.Date <= EndDate.Value.Date);
            }

            if (WalletId.HasValue)
            {
                query = query.Where(o => o.WalletId == WalletId.Value);
            }

            // تنفيذ الاستعلام وجلب البيانات مرتبة من الأحدث للأقدم
            ReportOperations = await query.OrderByDescending(o => o.Timestamp).ToListAsync();

            // حساب الإحصائيات للفترة المحددة
            TotalDeposits = ReportOperations.Where(o => o.OperationType == "إيداع").Sum(o => o.Amount);
            TotalWithdrawals = ReportOperations.Where(o => o.OperationType == "سحب").Sum(o => o.Amount);
            TotalCommissions = ReportOperations.Sum(o => o.Commission);
        }
    }
}