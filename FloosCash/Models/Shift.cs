using Azure;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FloosCash.Models
{
    public class Shift
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string EmployeeName { get; set; } = string.Empty;

        public DateTime StartTime { get; set; } = DateTime.Now;
        public DateTime? EndTime { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal OpeningCash { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal ClosingCash { get; set; }

        public bool IsClosed { get; set; } = false;

        // العلاقة: الوردية الواحدة تحتوي على عدة عمليات
        public ICollection<Operation> Operations { get; set; } = new List<Operation>();

        // المحافظ المسموح للموظف التعامل معها في هذه الوردية
        public ICollection<Wallet> AllowedWallets { get; set; } = new List<Wallet>();

        // قائمة الموظفين (الكاشيرية) المربوطين بهذه الوردية
        public ICollection<User> Cashiers { get; set; } = new List<User>();

    }
}