using Entities.Concrete;

namespace Business.Security.Jwt
{
    public interface ITokenHelper
    {
        AccessToken CreateToken(User user);
    }
}
