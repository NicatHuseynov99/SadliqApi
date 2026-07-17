using Core.DataAccess.EntityFramework;
using DataAccess.Abstract;
using DataAccess.Concrete.EntityFramework.Contexts;
using Entities.Concrete;

namespace DataAccess.Concrete
{
    public class EfHallDal : EfEntityRepositoryBase<Hall, SadliqDbContext>, IHallDal
    {
        public EfHallDal(SadliqDbContext context) : base(context)
        {
        }
    }
}
