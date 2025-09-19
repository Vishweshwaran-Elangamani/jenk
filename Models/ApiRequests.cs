namespace ReferralManagement.Models
{
    public class AddUserRequest
    {
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? Role { get; set; } // HR or Employee
        public string? CurrentProject { get; set; }
        public string? PlaceOfWork { get; set; }
        public string? Designation { get; set; }
    }

    public class SubmitReferralRequest
    {
        public string? CandidateName { get; set; }
        public string? CurrentCompany { get; set; }
        public string? CandidateEmail { get; set; }
        public int JobId { get; set; }
        public string? ResumePdfBase64 { get; set; } // For simplicity
    }

    public class ChangePasswordRequest
    {
        public int UserId { get; set; } // Used by HRController
    public string? Email { get; set; } // Used by AuthController
    public string? OldPassword { get; set; }
    public string? NewPassword { get; set; }
    public string? ConfirmNewPassword { get; set; }
    }

    public class UpdateReferralStatusRequest
    {
        public int ReferralId { get; set; }
    public string? NewStatus { get; set; } // Verified, Interview Scheduled, Confirmed, Rejected
        public DateTime? InterviewDateTime { get; set; }
    }


    public class SetReferralLimitRequest
    {
        public int EmployeeId { get; set; }
        public int LimitCount { get; set; }
    }

    public class AddJobRequest
    {
    public string? Title { get; set; }
    public string? Description { get; set; }
        public decimal ReferralBonus { get; set; }
        public int CreatedBy { get; set; }
    }
}
