using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FloosCash.Models
{
    public class Wallet
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "اسم المحفظة مطلوب")]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Type { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18, 2)")]
        public decimal CurrentBalance { get; set; }

        // --- العمولات ---
        [Column(TypeName = "decimal(5, 2)")]
        public decimal DepositCommissionRate { get; set; } // نسبة أو مبلغ عمولة الإيداع

        [Column(TypeName = "decimal(5, 2)")]
        public decimal WithdrawalCommissionRate { get; set; } // نسبة أو مبلغ عمولة السحب

        // --- الحدود اليومية ---
        [Column(TypeName = "decimal(18, 2)")]
        public decimal DailyDepositLimit { get; set; } = 60000; // حد الإيداع اليومي (افتراضي 60 ألف)

        [Column(TypeName = "decimal(18, 2)")]
        public decimal DailyWithdrawalLimit { get; set; } = 60000; // حد السحب اليومي

        // --- الحدود الشهرية ---
        [Column(TypeName = "decimal(18, 2)")]
        public decimal MonthlyDepositLimit { get; set; } = 200000; // حد الإيداع الشهري (افتراضي 200 ألف)

        [Column(TypeName = "decimal(18, 2)")]
        public decimal MonthlyWithdrawalLimit { get; set; } = 200000; // حد السحب الشهري

        public bool IsActive { get; set; } = true;

        // الورديات التي تم السماح لها باستخدام هذه المحفظة
        public ICollection<Shift> Shifts { get; set; } = new List<Shift>();
    }
}