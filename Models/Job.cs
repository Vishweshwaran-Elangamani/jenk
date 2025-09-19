using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ReferralManagement.Models
{
    public class Job
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Title { get; set; }
        [Required]
        public string Description { get; set; }
        [Required]
        public decimal ReferralBonus { get; set; }
    [ForeignKey("User")]
    public int? CreatedBy { get; set; }
    // User navigation property is not required for job creation and will not be validated
    [JsonIgnore]
    public User? User { get; set; } // Optional, not required for job creation
    }
}
