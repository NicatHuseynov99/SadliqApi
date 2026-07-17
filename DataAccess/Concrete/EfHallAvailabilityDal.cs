using Core.DataAccess.EntityFramework;
using DataAccess.Abstract;
using DataAccess.Concrete.EntityFramework.Contexts;
using Entities.Concrete;

namespace DataAccess.Concrete
{
    public class EfHallAvailabilityDal : EfEntityRepositoryBase<HallAvailability, SadliqDbContext>, IHallAvailabilityDal
    {
        public EfHallAvailabilityDal(SadliqDbContext context) : base(context)
        {
        }
    }
}
