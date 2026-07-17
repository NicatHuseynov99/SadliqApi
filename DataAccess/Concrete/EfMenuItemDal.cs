using Core.DataAccess.EntityFramework;
using DataAccess.Abstract;
using DataAccess.Concrete.EntityFramework.Contexts;
using Entities.Concrete;

namespace DataAccess.Concrete
{
    public class EfMenuItemDal : EfEntityRepositoryBase<MenuItem, SadliqDbContext>, IMenuItemDal
    {
        public EfMenuItemDal(SadliqDbContext context) : base(context)
        {
        }
    }
}
