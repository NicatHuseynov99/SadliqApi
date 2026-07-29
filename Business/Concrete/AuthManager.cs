using Business.Abstract;
using Business.Security.Jwt;
using Business.Utilities.Email;
using Core.Utilities.Results;
using Core.Utilities.Security.Hashing;
using DataAccess.Abstract;
using Entities.Concrete;
using Entities.Dtos.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace Business.Concrete
{
    public class AuthManager : IAuthService
    {
        // Sıfırlama linkinin etibarlılıq müddəti
        private static readonly TimeSpan ResetTokenLifetime = TimeSpan.FromHours(2);

        private readonly IUserDal _userDal;
        private readonly ITokenHelper _tokenHelper;
        private readonly IPasswordResetTokenDal _resetTokenDal;
        private readonly IEmailService _emailService;
        private readonly EmailOptions _emailOptions;

        public AuthManager(
            IUserDal userDal,
            ITokenHelper tokenHelper,
            IPasswordResetTokenDal resetTokenDal,
            IEmailService emailService,
            IOptions<EmailOptions> emailOptions)
        {
            _userDal = userDal;
            _tokenHelper = tokenHelper;
            _resetTokenDal = resetTokenDal;
            _emailService = emailService;
            _emailOptions = emailOptions.Value;
        }

        // Yeni istifadəçi qeydiyyatı: parolu hashləyib bazaya yazır
        public async Task<IResult> RegisterAsync(RegisterDto dto)
        {
            var existingUser = await _userDal.GetAsync(u => u.Email == dto.Email);
            if (existingUser != null)
                return new ErrorResult("Bu email artıq qeydiyyatdan keçib.");

            HashingHelper.CreatePasswordHash(dto.Password, out var passwordHash, out var passwordSalt);

            // Admin rolu API ilə açılmır — yalnız Customer/Owner seçilə bilər
            if (!Enum.TryParse<UserRole>(dto.Role, true, out var role) || role == UserRole.Admin)
                role = UserRole.Customer;

            var user = new User
            {
                FirstName = dto.FirstName.Trim(),
                LastName = dto.LastName.Trim(),
                Email = dto.Email,
                Phone = dto.Phone,
                PasswordHash = passwordHash,
                PasswordSalt = passwordSalt,
                Role = role,
                CreatedAt = DateTime.UtcNow
            };

            await _userDal.AddAsync(user);
            return new SuccessResult("Qeydiyyat uğurludur.");
        }

        // İstifadəçi məlumatlarını yoxlayıb JWT token qaytarır
        public async Task<IDataResult<TokenResponseDto>> LoginAsync(LoginDto dto)
        {
            var user = await _userDal.GetAsync(u => u.Email == dto.Email);
            if (user == null)
                return new ErrorDataResult<TokenResponseDto>("İstifadəçi tapılmadı.");

            if (!HashingHelper.VerifyPasswordHash(dto.Password, user.PasswordHash, user.PasswordSalt))
                return new ErrorDataResult<TokenResponseDto>("Parol yanlışdır.");

            var token = _tokenHelper.CreateToken(user);

            var response = new TokenResponseDto
            {
                Token = token.Token,
                Expiration = token.Expiration
            };

            return new SuccessDataResult<TokenResponseDto>(response, "Giriş uğurludur.");
        }

        // Token-dəki istifadəçinin öz profilini qaytarır
        public async Task<IDataResult<User>> GetMeAsync(int userId)
        {
            var user = await _userDal.GetAsync(u => u.Id == userId);
            return user is null
                ? new ErrorDataResult<User>("İstifadəçi tapılmadı.")
                : new SuccessDataResult<User>(user);
        }

        // Ad və telefonu yeniləyir; email və rol dəyişdirilmir
        public async Task<IDataResult<User>> UpdateProfileAsync(int userId, UpdateProfileDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.FirstName))
                return new ErrorDataResult<User>("Ad boş ola bilməz.");

            if (string.IsNullOrWhiteSpace(dto.LastName))
                return new ErrorDataResult<User>("Soyad boş ola bilməz.");

            if (string.IsNullOrWhiteSpace(dto.Phone))
                return new ErrorDataResult<User>("Telefon nömrəsi boş ola bilməz.");

            var user = await _userDal.GetAsync(u => u.Id == userId);
            if (user is null)
                return new ErrorDataResult<User>("İstifadəçi tapılmadı.");

            user.FirstName = dto.FirstName.Trim();
            user.LastName = dto.LastName.Trim();
            user.Phone = dto.Phone.Trim();
            await _userDal.UpdateAsync(user);

            return new SuccessDataResult<User>(user, "Məlumatlarınız yeniləndi.");
        }

        // Cari parolu yoxlayıb yenisini hashləyir
        public async Task<IResult> ChangePasswordAsync(int userId, ChangePasswordDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.NewPassword) || dto.NewPassword.Length < 6)
                return new ErrorResult("Yeni parol ən azı 6 simvol olmalıdır.");

            var user = await _userDal.GetAsync(u => u.Id == userId);
            if (user is null)
                return new ErrorResult("İstifadəçi tapılmadı.");

            if (!HashingHelper.VerifyPasswordHash(dto.CurrentPassword, user.PasswordHash, user.PasswordSalt))
                return new ErrorResult("Cari parol yanlışdır.");

            if (HashingHelper.VerifyPasswordHash(dto.NewPassword, user.PasswordHash, user.PasswordSalt))
                return new ErrorResult("Yeni parol köhnə parolla eyni ola bilməz.");

            HashingHelper.CreatePasswordHash(dto.NewPassword, out var passwordHash, out var passwordSalt);
            user.PasswordHash = passwordHash;
            user.PasswordSalt = passwordSalt;
            await _userDal.UpdateAsync(user);

            return new SuccessResult("Parolunuz dəyişdirildi.");
        }

        // Sıfırlama linki göndərir. Cavab həmişə eynidir — kimin qeydiyyatda olduğu
        // bu yolla öyrənilə bilməsin deyə (user enumeration qorunması)
        public async Task<IResult> ForgotPasswordAsync(ForgotPasswordDto dto)
        {
            const string genericMessage =
                "Əgər bu e-poçt qeydiyyatdan keçibsə, parol sıfırlama linki göndərildi.";

            if (string.IsNullOrWhiteSpace(dto.Email))
                return new ErrorResult("E-poçt ünvanını yazın.");

            var user = await _userDal.GetAsync(u => u.Email == dto.Email.Trim());
            if (user is null)
                return new SuccessResult(genericMessage);

            // Köhnə istifadə olunmamış tokenləri ləğv edirik ki, eyni anda bir neçə link işləməsin
            var oldTokens = await _resetTokenDal.GetListAsync(t => t.UserId == user.Id && t.UsedAt == null);
            foreach (var old in await oldTokens.ToListAsync())
            {
                old.UsedAt = DateTime.UtcNow;
                await _resetTokenDal.UpdateAsync(old);
            }

            var rawToken = GenerateToken();

            await _resetTokenDal.AddAsync(new PasswordResetToken
            {
                UserId = user.Id,
                TokenHash = HashToken(rawToken),
                ExpiresAt = DateTime.UtcNow.Add(ResetTokenLifetime),
                CreatedAt = DateTime.UtcNow
            });

            var link = $"{_emailOptions.FrontendUrl.TrimEnd('/')}/reset-password?token={rawToken}";
            await _emailService.SendAsync(user.Email, "Parolun bərpası", BuildResetEmail(user.FirstName, link));

            return new SuccessResult(genericMessage);
        }

        // Linkdəki token ilə yeni parol təyin edir
        public async Task<IResult> ResetPasswordAsync(ResetPasswordDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.NewPassword) || dto.NewPassword.Length < 6)
                return new ErrorResult("Yeni parol ən azı 6 simvol olmalıdır.");

            if (string.IsNullOrWhiteSpace(dto.Token))
                return new ErrorResult("Sıfırlama linki keçərsizdir.");

            var tokenHash = HashToken(dto.Token);
            var token = await _resetTokenDal.GetAsync(t => t.TokenHash == tokenHash);

            if (token is null || token.UsedAt != null)
                return new ErrorResult("Bu link artıq istifadə olunub və ya keçərsizdir.");

            if (token.ExpiresAt < DateTime.UtcNow)
                return new ErrorResult("Linkin vaxtı bitib. Yenidən sıfırlama sorğusu göndərin.");

            var user = await _userDal.GetAsync(u => u.Id == token.UserId);
            if (user is null)
                return new ErrorResult("İstifadəçi tapılmadı.");

            HashingHelper.CreatePasswordHash(dto.NewPassword, out var passwordHash, out var passwordSalt);
            user.PasswordHash = passwordHash;
            user.PasswordSalt = passwordSalt;
            await _userDal.UpdateAsync(user);

            // Token birdəfəlikdir
            token.UsedAt = DateTime.UtcNow;
            await _resetTokenDal.UpdateAsync(token);

            return new SuccessResult("Parolunuz yeniləndi, indi daxil ola bilərsiniz.");
        }

        // Kriptoqrafik təsadüfi token (URL-də təhlükəsiz görünüş üçün base64url)
        private static string GenerateToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(32);
            return Convert.ToBase64String(bytes)
                .Replace("+", "-").Replace("/", "_").TrimEnd('=');
        }

        // Bazada token-in özü yox, hash-i saxlanılır
        private static string HashToken(string token)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            return Convert.ToHexString(bytes);
        }

        private static string BuildResetEmail(string firstName, string link) => $@"
            <div style='font-family:Segoe UI,Arial,sans-serif;color:#1a1118'>
              <h2 style='color:#721438'>Parolun bərpası</h2>
              <p>Salam, {firstName}!</p>
              <p>Hesabınızın parolunu sıfırlamaq üçün aşağıdakı düyməyə klikləyin.
                 Link 2 saat ərzində etibarlıdır.</p>
              <p style='margin:24px 0'>
                <a href='{link}' style='background:#721438;color:#fff;padding:12px 24px;
                   border-radius:8px;text-decoration:none;font-weight:600'>Parolu sıfırla</a>
              </p>
              <p style='color:#868e96;font-size:13px'>
                Bu sorğunu siz göndərməmisinizsə, bu məktubu nəzərə almayın — parolunuz dəyişməyəcək.
              </p>
            </div>";
    }
}
