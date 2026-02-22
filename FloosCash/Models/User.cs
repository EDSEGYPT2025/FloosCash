using System.ComponentModel.DataAnnotations;

namespace FloosCash.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "الاسم بالكامل مطلوب")]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "اسم المستخدم مطلوب")]
        [MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "كلمة المرور مطلوبة")]
        public string Password { get; set; } = string.Empty; // ملاحظة: مستقبلاً يفضل تشفيرها

        [Required]
        [MaxLength(20)]
        public string Role { get; set; } = "Cashier"; // القيم ستكون Admin أو Cashier

        public bool IsActive { get; set; } = true;

        // الورديات التي شارك فيها هذا الموظف
        public ICollection<Shift> Shifts { get; set; } = new List<Shift>();
    }
}