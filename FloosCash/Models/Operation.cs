using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FloosCash.Models
{
    public class Operation
    {
        public int Id { get; set; }

        // ربط العملية بالوردية
        public int ShiftId { get; set; }
        public Shift Shift { get; set; } = null!;

        // ربط العملية بالمحفظة
        public int WalletId { get; set; }
        public Wallet Wallet { get; set; } = null!;

        [Required]
        [MaxLength(50)]
        public string OperationType { get; set; } = string.Empty; // إيداع، سحب، تحويل

        [Column(TypeName = "decimal(18, 2)")]
        public decimal Amount { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal Commission { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.Now;

        [MaxLength(255)]
        public string Notes { get; set; } = string.Empty;
    }
}