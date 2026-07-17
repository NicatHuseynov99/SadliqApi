using Core.DataAccess.EntityFramework;
using DataAccess.Abstract;
using DataAccess.Concrete.EntityFramework.Contexts;
using Entities.Concrete;

namespace DataAccess.Concrete
{
    public class EfVenueDal : EfEntityRepositoryBase<Venue, SadliqDbContext>, IVenueDal
    {
        public EfVenueDal(SadliqDbContext context) : base(context)
        {
        }
    }
}
