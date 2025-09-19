namespace ReferralManagement.Models
{
    public class CreateUserRequest
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public string Project { get; set; }
        public string Workplace { get; set; }
        public string Designation { get; set; }
    }
}