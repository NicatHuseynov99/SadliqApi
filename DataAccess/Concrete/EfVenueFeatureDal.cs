using Core.DataAccess.EntityFramework;
using DataAccess.Abstract;
using DataAccess.Concrete.EntityFramework.Contexts;
using Entities.Concrete;

namespace DataAccess.Concrete
{
    public class EfVenueFeatureDal : EfEntityRepositoryBase<VenueFeature, SadliqDbContext>, IVenueFeatureDal
    {
        public EfVenueFeatureDal(SadliqDbContext context) : base(context)
        {
        }
    }
}
