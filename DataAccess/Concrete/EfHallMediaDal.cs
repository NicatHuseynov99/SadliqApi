using Core.DataAccess.EntityFramework;
using DataAccess.Abstract;
using DataAccess.Concrete.EntityFramework.Contexts;
using Entities.Concrete;

namespace DataAccess.Concrete
{
    public class EfHallMediaDal : EfEntityRepositoryBase<HallMedia, SadliqDbContext>, IHallMediaDal
    {
        public EfHallMediaDal(SadliqDbContext context) : base(context)
        {
        }
    }
}
