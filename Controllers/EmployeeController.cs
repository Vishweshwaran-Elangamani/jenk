using ReferralManagement.Models;
using Microsoft.AspNetCore.Mvc;
using ReferralManagement.Data;
using Microsoft.EntityFrameworkCore;

namespace ReferralManagement.Controllers
{
    [ApiController]
    [Route("api/employee")]
    public class EmployeeController : ControllerBase
    {
        private readonly ReferralDbContext _context;
        public EmployeeController(ReferralDbContext context)
        {
            _context = context;
        }


        // PATCH: api/employee/fix-submitted-date
        [HttpPatch("fix-submitted-date")]
        public async Task<IActionResult> FixSubmittedDate()
        {
            var referrals = await _context.Referrals.Where(r => r.SubmittedAt == null || r.SubmittedAt.Value.Year == 1970).ToListAsync();
            foreach (var referral in referrals)
            {
                referral.SubmittedAt = DateTime.UtcNow;
            }
            await _context.SaveChangesAsync();
            return Ok(new { updated = referrals.Count });
        }

        // GET: api/employee/jobs
        [HttpGet("jobs")]
        public async Task<IActionResult> GetJobs()
        {
            var jobs = await _context.Jobs.ToListAsync();
            return Ok(jobs);
        }

        // POST: api/employee/referral
        [HttpPost("referral")]
        public async Task<IActionResult> SubmitReferral([FromBody] Referral referral)
        {
            if (referral == null || string.IsNullOrEmpty(referral.CandidateName) || string.IsNullOrEmpty(referral.CandidateEmail) || referral.JobId == 0 || referral.EmployeeId == 0)
                return BadRequest("Missing required fields");
            var limit = await _context.ReferralLimits.FirstOrDefaultAsync(l => l.EmployeeId == referral.EmployeeId);
            if (limit != null && limit.UsedCount >= limit.LimitCount)
                return BadRequest($"Referral limit reached ({limit.LimitCount})");
            referral.Status = "Pending";
            // Set submittedAt as ISO 8601 string for frontend compatibility
            referral.SubmittedAt = DateTime.UtcNow;
            _context.Referrals.Add(referral);
            if (limit != null)
                limit.UsedCount++;
            await _context.SaveChangesAsync();
            return Ok(new { success = true, referralId = referral.Id });
        }

        // GET: api/employee/referrals?employeeId=123
        [HttpGet("referrals")]
        public async Task<IActionResult> GetReferrals([FromQuery] int employeeId)
        {
            if (employeeId == 0)
                return BadRequest("EmployeeId not found");
            var referrals = await _context.Referrals
                .Where(r => r.EmployeeId == employeeId)
                .Include(r => r.Job)
                .ToListAsync();
            return Ok(referrals);
        }


        // DELETE: api/employee/referral/{id}?employeeId=123
        [HttpDelete("referral/{id}")]
        public async Task<IActionResult> DeleteReferral(int id, [FromQuery] int employeeId)
        {
            var referral = await _context.Referrals.FirstOrDefaultAsync(r => r.Id == id && r.EmployeeId == employeeId);
            if (referral == null)
                return NotFound("Referral not found or not owned by employee");
            _context.Referrals.Remove(referral);
            // Decrement used count if referral limit exists
            var limit = await _context.ReferralLimits.FirstOrDefaultAsync(l => l.EmployeeId == employeeId);
            if (limit != null && limit.UsedCount > 0)
                limit.UsedCount--;
            await _context.SaveChangesAsync();
            return Ok(new { success = true });
        }


        // GET: api/employee/earnings?employeeId=123
        [HttpGet("earnings")]
        public async Task<IActionResult> GetEarnings([FromQuery] int employeeId)
        {
            if (employeeId == 0)
                return BadRequest("EmployeeId not found");
            var earnings = await _context.Earnings
                .Include(e => e.Referral)
                .Where(e => e.Referral.EmployeeId == employeeId)
                .ToListAsync();
            return Ok(earnings);
        }


        // POST: api/employee/change-password?employeeId=123
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, [FromQuery] int employeeId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == employeeId);
            if (user == null)
                return NotFound("User not found");
            if (string.IsNullOrEmpty(request.OldPassword) || string.IsNullOrEmpty(request.NewPassword) || string.IsNullOrEmpty(request.ConfirmNewPassword))
                return BadRequest("All password fields are required");
            if (user.Password != request.OldPassword)
                return BadRequest("Current password is incorrect");
            if (request.NewPassword.Length < 6)
                return BadRequest("New password must be at least 6 characters");
            if (request.NewPassword != request.ConfirmNewPassword)
                return BadRequest("New password and confirm password do not match");
            user.Password = request.NewPassword;
            user.FirstLogin = false;
            await _context.SaveChangesAsync();
            return Ok(new { success = true });
        }

        // Upload referral with PDF as BLOB
        // POST: api/employee/referral-with-pdf
        [HttpPost("referral-with-pdf")]
        public async Task<IActionResult> SubmitReferralWithPdf()
        {
            var form = await Request.ReadFormAsync();
            var file = form.Files["resume"];
            var candidateName = form["candidateName"].ToString();
            var currentCompany = form["currentCompany"].ToString();
            var candidateEmail = form["candidateEmail"].ToString();
            var jobId = int.Parse(form["jobId"]);
            var employeeId = int.Parse(form["employeeId"]);
            var status = "Pending";
            var submittedAt = DateTime.UtcNow;
            if (string.IsNullOrEmpty(candidateName) || string.IsNullOrEmpty(candidateEmail) || jobId == 0 || employeeId == 0 || file == null)
                return BadRequest("Missing required fields or file");
            var limit = await _context.ReferralLimits.FirstOrDefaultAsync(l => l.EmployeeId == employeeId);
            if (limit != null && limit.UsedCount >= limit.LimitCount)
                return BadRequest($"Referral limit reached ({limit.LimitCount})");
            byte[] fileBytes;
            using (var ms = new MemoryStream())
            {
                await file.CopyToAsync(ms);
                fileBytes = ms.ToArray();
            }
            var referral = new Referral
            {
                CandidateName = candidateName,
                CurrentCompany = currentCompany,
                CandidateEmail = candidateEmail,
                ResumePdf = file.FileName,
                ResumeBlob = fileBytes,
                JobId = jobId,
                EmployeeId = employeeId,
                Status = status,
                InterviewDateTime = null,
                SubmittedAt = submittedAt
            };
            _context.Referrals.Add(referral);
            if (limit != null)
                limit.UsedCount++;
            await _context.SaveChangesAsync();
            return Ok(new { success = true, referralId = referral.Id });
        }

        // Download PDF by referral ID
        // GET: api/employee/referral-pdf/{id}
        [HttpGet("referral-pdf/{id}")]
        public async Task<IActionResult> DownloadReferralPdf(int id)
        {
            var referral = await _context.Referrals.FirstOrDefaultAsync(r => r.Id == id);
            if (referral == null || referral.ResumeBlob == null)
                return NotFound("Resume not found");
            return File(referral.ResumeBlob, "application/pdf", referral.ResumePdf ?? "resume.pdf");
        }
        // (Removed duplicate endpoints below. Only one version of each endpoint remains above.)
    }
}
