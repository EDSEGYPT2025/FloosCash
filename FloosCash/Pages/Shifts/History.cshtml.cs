using FloosCash.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FloosCash.Pages.Shifts
{
    [Authorize(Roles = "Admin")]
    public class HistoryModel : PageModel
    {
        private readonly AppDbContext _context;
        public HistoryModel(AppDbContext context) => _context = context;

        public List<ShiftSummary> ClosedShifts { get; set; } = new List<ShiftSummary>();

        public async Task OnGetAsync()
        {
            // جلب الورديات المغلقة فقط مرتبة من الأحدث للأقدم
            var shifts = await _context.Shifts
                .Include(s => s.Operations)
                .Where(s => s.IsClosed)
                .OrderByDescending(s => s.EndTime)
                .ToListAsync();

            foreach (var s in shifts)
            {
                var deposits = s.Operations.Where(o => o.OperationType == "إيداع").Sum(o => o.Amount);
                var withdrawals = s.Operations.Where(o => o.OperationType == "سحب").Sum(o => o.Amount);
                var comms = s.Operations.Sum(o => o.Commission);

                var expected = s.OpeningCash + deposits - withdrawals + comms;
                var diff = s.ClosingCash - expected;

                ClosedShifts.Add(new ShiftSummary
                {
                    Id = s.Id,
                    EmployeeName = s.EmployeeName,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                    ExpectedCash = expected,
                    ActualCash = s.ClosingCash,
                    Difference = diff
                });
            }
        }
    }

    public class ShiftSummary
    {
        public int Id { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public decimal ExpectedCash { get; set; }
        public decimal ActualCash { get; set; }
        public decimal Difference { get; set; }
    }
}