using Core.DataAccess;
using Entities.Concrete;

namespace DataAccess.Abstract
{
    public interface IVenueRequestDal : IEntityRepository<VenueRequest>
    {
    }

    public interface IVenueOfferDal : IEntityRepository<VenueOffer>
    {
    }
}
