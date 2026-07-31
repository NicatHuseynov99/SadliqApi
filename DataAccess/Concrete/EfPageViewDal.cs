using Core.DataAccess.EntityFramework;
using DataAccess.Abstract;
using DataAccess.Concrete.EntityFramework.Contexts;
using Entities.Concrete;

namespace DataAccess.Concrete
{
    public class EfPageViewDal : EfEntityRepositoryBase<PageView, SadliqDbContext>, IPageViewDal
    {
        public EfPageViewDal(SadliqDbContext context) : base(context)
        {
        }
    }
}
