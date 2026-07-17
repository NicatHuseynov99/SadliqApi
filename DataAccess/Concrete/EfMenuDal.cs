using Core.DataAccess.EntityFramework;
using DataAccess.Abstract;
using DataAccess.Concrete.EntityFramework.Contexts;
using Entities.Concrete;

namespace DataAccess.Concrete
{
    public class EfMenuDal : EfEntityRepositoryBase<Menu, SadliqDbContext>, IMenuDal
    {
        public EfMenuDal(SadliqDbContext context) : base(context)
        {
        }
    }
}
