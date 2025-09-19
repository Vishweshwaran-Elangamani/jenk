using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ReferralManagement.Models
{
    public class ReferralLimit
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int EmployeeId { get; set; } // auto-increment PK

        [Required]
        public int EmployeeId1 { get; set; } // actual employee key

        [Required]
        public int LimitCount { get; set; }
        public int UsedCount { get; set; } = 0;
    }

    public class ReferralLimitDto
    {
        public int EmployeeId { get; set; }
        public int? EmployeeId1 { get; set; }
        public int LimitCount { get; set; }
        public int UsedCount { get; set; } = 0;
    }
}
