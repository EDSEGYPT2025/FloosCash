using FloosCash.Data;
using FloosCash.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FloosCash.Pages.Shifts
{
    public class CreateModel : PageModel
    {
        private readonly AppDbContext _context;

        public CreateModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Shift Shift { get; set; } = default!;

        public void OnGet()
        {
            // تجهيز بيانات افتراضية لتسريع العمل
            Shift = new Shift
            {
                EmployeeName = "أحمد سمير", // اسم افتراضي للتجربة
                StartTime = DateTime.Now
            };
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // التأكد من ضبط القيم الأساسية قبل الحفظ
            Shift.StartTime = DateTime.Now;
            Shift.IsClosed = false; // الوردية مفتوحة حالياً
            Shift.ClosingCash = 0;  // سيتم حسابه عند إغلاق الوردية

            _context.Shifts.Add(Shift);
            await _context.SaveChangesAsync();

            // بعد فتح الوردية، نعود للشاشة الرئيسية (الداشبورد)
            return RedirectToPage("/Index");
        }
    }
}