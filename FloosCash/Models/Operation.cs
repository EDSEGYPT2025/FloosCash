using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FloosCash.Models
{
    public class Operation
    {
        public int Id { get; set; }

        // ربط العملية بالوردية
        public int ShiftId { get; set; }

        // التعديل: أضفنا ? لكي لا يطلب النظام بيانات الوردية من الفورم
        public Shift? Shift { get; set; }

        // ربط العملية بالمحفظة
        [Required(ErrorMessage = "عفواً، يرجى اختيار المحفظة أولاً.")]
        public int WalletId { get; set; }

        // التعديل: أضفنا ? لكي لا يطلب النظام بيانات المحفظة كاملة من الفورم
        public Wallet? Wallet { get; set; }

        [Required(ErrorMessage = "عفواً، يرجى تحديد نوع العملية (إيداع أم سحب).")]
        [MaxLength(50)]
        public string OperationType { get; set; } = string.Empty; // إيداع، سحب، تحويل

        [Required(ErrorMessage = "عفواً، يرجى كتابة مبلغ العملية.")]
        [Range(1, double.MaxValue, ErrorMessage = "يجب أن يكون المبلغ أكبر من صفر.")]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal Amount { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal Commission { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.Now;

        // التعديل: أضفنا ? لكي يصبح الحقل اختيارياً تماماً ولا تظهر عليه رسالة The Notes field is required
        [MaxLength(255)]
        public string? Notes { get; set; }
    }
}