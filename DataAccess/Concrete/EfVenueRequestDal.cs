using Core.DataAccess.EntityFramework;
using DataAccess.Abstract;
using DataAccess.Concrete.EntityFramework.Contexts;
using Entities.Concrete;

namespace DataAccess.Concrete
{
    public class EfVenueRequestDal : EfEntityRepositoryBase<VenueRequest, SadliqDbContext>, IVenueRequestDal
    {
        public EfVenueRequestDal(SadliqDbContext context) : base(context)
        {
        }
    }

    public class EfVenueOfferDal : EfEntityRepositoryBase<VenueOffer, SadliqDbContext>, IVenueOfferDal
    {
        public EfVenueOfferDal(SadliqDbContext context) : base(context)
        {
        }
    }
}
