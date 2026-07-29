using Core.DataAccess.EntityFramework;
using DataAccess.Abstract;
using DataAccess.Concrete.EntityFramework.Contexts;
using Entities.Concrete;

namespace DataAccess.Concrete
{
    public class EfPasswordResetTokenDal : EfEntityRepositoryBase<PasswordResetToken, SadliqDbContext>, IPasswordResetTokenDal
    {
        public EfPasswordResetTokenDal(SadliqDbContext context) : base(context)
        {
        }
    }
}
