using Core.DataAccess.EntityFramework;
using DataAccess.Abstract;
using DataAccess.Concrete.EntityFramework.Contexts;
using Entities.Concrete;

namespace DataAccess.Concrete
{
    public class EfUserDal : EfEntityRepositoryBase<User, SadliqDbContext>, IUserDal
    {
        public EfUserDal(SadliqDbContext context) : base(context)
        {
        }
    }
}
