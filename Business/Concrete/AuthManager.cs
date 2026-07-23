using Business.Abstract;
using Business.Security.Jwt;
using Core.Utilities.Results;
using Core.Utilities.Security.Hashing;
using DataAccess.Abstract;
using Entities.Concrete;
using Entities.Dtos.Auth;

namespace Business.Concrete
{
    public class AuthManager : IAuthService
    {
        private readonly IUserDal _userDal;
        private readonly ITokenHelper _tokenHelper;

        public AuthManager(IUserDal userDal, ITokenHelper tokenHelper)
        {
            _userDal = userDal;
            _tokenHelper = tokenHelper;
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
                FullName = dto.FullName,
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
    }
}
