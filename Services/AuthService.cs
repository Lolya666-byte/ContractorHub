using System.Security.Cryptography;
using System.Text;
using ContractorHub.Data;
using ContractorHub.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ContractorHub.Services
{
    public class AuthService
    {
        private readonly AppDbContext _context;
        private readonly PasswordHasher<User> _passwordHasher;

        public AuthService(AppDbContext context)
        {
            _context = context;
            _passwordHasher = new PasswordHasher<User>();
        }

        public string HashPassword(User user, string password)
        {
            return _passwordHasher.HashPassword(user, password);
        }

        public bool VerifyPassword(User user, string password)
        {
            var result = _passwordHasher.VerifyHashedPassword(
                user,
                user.PasswordHash,
                password);

            return result == PasswordVerificationResult.Success ||
                   result == PasswordVerificationResult.SuccessRehashNeeded;
        }

        public bool IsLegacyHash(string passwordHash)
        {
            return passwordHash.Length == 64 &&
                   passwordHash.All(c =>
                       c is >= '0' and <= '9' or
                       >= 'A' and <= 'F' or
                       >= 'a' and <= 'f');
        }

        private bool VerifyLegacyPassword(string password, string passwordHash)
        {
            using var sha256 = SHA256.Create();

            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha256.ComputeHash(bytes);
            var calculated = Convert.ToHexString(hash);

            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(calculated),
                Encoding.UTF8.GetBytes(passwordHash));
        }

        public bool IsPasswordStrongEnough(string password, out string error)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                error = "Введите пароль.";
                return false;
            }

            if (password.Length < 8)
            {
                error = "Пароль должен содержать не менее 8 символов.";
                return false;
            }

            if (!password.Any(char.IsUpper))
            {
                error = "Пароль должен содержать хотя бы одну заглавную букву.";
                return false;
            }

            if (!password.Any(char.IsLower))
            {
                error = "Пароль должен содержать хотя бы одну строчную букву.";
                return false;
            }

            if (!password.Any(char.IsDigit))
            {
                error = "Пароль должен содержать хотя бы одну цифру.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        public async Task<(User? User, bool RequiresPasswordChange)> AuthenticateAsync(
            string username,
            string password)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(x =>
                    x.Username == username &&
                    x.IsActive);

            if (user == null)
                return (null, false);

            if (IsLegacyHash(user.PasswordHash))
            {
                if (!VerifyLegacyPassword(password, user.PasswordHash))
                    return (null, false);

                user.PasswordHash = HashPassword(user, password);

                await _context.SaveChangesAsync();

                return (user, true);
            }

            var result = _passwordHasher.VerifyHashedPassword(
                user,
                user.PasswordHash,
                password);

            if (result == PasswordVerificationResult.Failed)
                return (null, false);

            if (result == PasswordVerificationResult.SuccessRehashNeeded)
            {
                user.PasswordHash = HashPassword(user, password);
                await _context.SaveChangesAsync();
            }

            return (user, false);
        }
    }
}
