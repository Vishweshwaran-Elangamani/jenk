using Microsoft.AspNetCore.Mvc;
using ReferralManagement.Data;
using ReferralManagement.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace ReferralManagement.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly ReferralDbContext _context;
        public AuthController(ReferralDbContext context)
        {
            _context = context;
        }

        // POST: api/auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest req)
        {
            // Hardcoded demo credentials
            if (req.Email == "admin@company.com" && req.Password == "admin123")
            {
                return Ok(new { Id = 0, Name = "Admin", Email = req.Email, Role = "Admin", FirstLogin = false });
            }
            if (req.Email == "sarah@company.com" && req.Password == "employee123")
            {
                return Ok(new { Id = 1, Name = "Sarah", Email = req.Email, Role = "Employee", FirstLogin = false });
            }
            if (req.Email == "mike@company.com" && req.Password == "hr123")
            {
                return Ok(new { Id = 2, Name = "Mike", Email = req.Email, Role = "HR", FirstLogin = false });
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == req.Email);
            if (user == null)
            {
                Console.WriteLine($"Login failed: User not found for email {req.Email}");
                return Unauthorized("Invalid credentials");
            }
            if (req.Password != user.Password)
            {
                Console.WriteLine($"Login failed: Incorrect password for email {req.Email}");
                return Unauthorized("Invalid credentials");
            }
            return Ok(new { user.Id, user.Name, user.Email, user.Role, user.FirstLogin });
        }

        // POST: api/auth/change-password
        [HttpPost("change-password")]
                public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest req)
                {
                    Console.WriteLine("[ChangePassword] Endpoint called");
            Console.WriteLine($"[ChangePassword] Incoming: Email={req.Email}, OldPassword={req.OldPassword}, NewPassword={req.NewPassword}, ConfirmNewPassword={req.ConfirmNewPassword}");
            if (string.IsNullOrEmpty(req.Email) || string.IsNullOrEmpty(req.OldPassword) || string.IsNullOrEmpty(req.NewPassword) || string.IsNullOrEmpty(req.ConfirmNewPassword))
            {
                Console.WriteLine("[ChangePassword] All password fields are required");
                return BadRequest("All password fields are required");
            }
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == req.Email);
            if (user == null)
            {
                Console.WriteLine($"[ChangePassword] User not found for email {req.Email}");
                return NotFound("User not found");
            }
            if (user.Password != req.OldPassword)
            {
                Console.WriteLine("[ChangePassword] Current password is incorrect");
                return BadRequest("Current password is incorrect");
            }
            if (req.NewPassword.Length < 6)
            {
                Console.WriteLine("[ChangePassword] New password must be at least 6 characters");
                return BadRequest("New password must be at least 6 characters");
            }
            if (req.NewPassword != req.ConfirmNewPassword)
            {
                Console.WriteLine("[ChangePassword] New password and confirm password do not match");
                return BadRequest("New password and confirm password do not match");
            }
            user.Password = req.NewPassword;
            user.FirstLogin = false;
            user.Name = user.Name + "_changed";
            var affectedRows = await _context.SaveChangesAsync();
            Console.WriteLine($"[ChangePassword] Rows affected: {affectedRows}");
            // Re-fetch user to confirm update
            var updatedUser = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == req.Email);
            Console.WriteLine($"[ChangePassword] Password in DB after update: {updatedUser?.Password}");
            Console.WriteLine($"[ChangePassword] Name in DB after update: {updatedUser?.Name}");
            Console.WriteLine($"[ChangePassword] Password updated for user {user.Email}");
            return Ok(new { success = true });
        }

    }

    public class LoginRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }
}
