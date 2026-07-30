using Core.DataAccess.EntityFramework;
using DataAccess.Abstract;
using DataAccess.Concrete.EntityFramework.Contexts;
using Entities.Concrete;

namespace DataAccess.Concrete
{
    public class EfVenueStaffHallDal : EfEntityRepositoryBase<VenueStaffHall, SadliqDbContext>, IVenueStaffHallDal
    {
        public EfVenueStaffHallDal(SadliqDbContext context) : base(context)
        {
        }
    }
}
