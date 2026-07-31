using Core.DataAccess.EntityFramework;
using DataAccess.Abstract;
using DataAccess.Concrete.EntityFramework.Contexts;
using Entities.Concrete;

namespace DataAccess.Concrete
{
    public class EfAppTextDal : EfEntityRepositoryBase<AppText, SadliqDbContext>, IAppTextDal
    {
        public EfAppTextDal(SadliqDbContext context) : base(context)
        {
        }
    }
}
