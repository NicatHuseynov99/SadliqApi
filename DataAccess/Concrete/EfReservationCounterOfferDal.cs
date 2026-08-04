using Core.DataAccess.EntityFramework;
using DataAccess.Abstract;
using DataAccess.Concrete.EntityFramework.Contexts;
using Entities.Concrete;

namespace DataAccess.Concrete
{
    public class EfReservationCounterOfferDal
        : EfEntityRepositoryBase<ReservationCounterOffer, SadliqDbContext>, IReservationCounterOfferDal
    {
        public EfReservationCounterOfferDal(SadliqDbContext context) : base(context)
        {
        }
    }
}
