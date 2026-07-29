using Core.DataAccess.EntityFramework;
using DataAccess.Abstract;
using DataAccess.Concrete.EntityFramework.Contexts;
using Entities.Concrete;

namespace DataAccess.Concrete
{
    public class EfVenueStaffDal : EfEntityRepositoryBase<VenueStaff, SadliqDbContext>, IVenueStaffDal
    {
        public EfVenueStaffDal(SadliqDbContext context) : base(context)
        {
        }
    }
}
