using Core.DataAccess.EntityFramework;
using DataAccess.Abstract;
using DataAccess.Concrete.EntityFramework.Contexts;
using Entities.Concrete;

namespace DataAccess.Concrete
{
    public class EfVenueImageDal : EfEntityRepositoryBase<VenueImage, SadliqDbContext>, IVenueImageDal
    {
        public EfVenueImageDal(SadliqDbContext context) : base(context)
        {
        }
    }
}
