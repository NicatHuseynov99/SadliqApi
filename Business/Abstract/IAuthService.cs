using Core.Utilities.Results;
using Entities.Concrete;
using Entities.Dtos.Auth;

namespace Business.Abstract
{
    public interface IAuthService
    {
        Task<IResult> RegisterAsync(RegisterDto dto);
        Task<IDataResult<TokenResponseDto>> LoginAsync(LoginDto dto);
        Task<IDataResult<User>> GetMeAsync(int userId);
    }
}
