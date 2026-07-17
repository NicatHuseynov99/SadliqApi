using Core.DataAccess.EntityFramework;
using DataAccess.Abstract;
using DataAccess.Concrete.EntityFramework.Contexts;
using Entities.Concrete;

namespace DataAccess.Concrete
{
    public class EfReservationDal : EfEntityRepositoryBase<Reservation, SadliqDbContext>, IReservationDal
    {
        public EfReservationDal(SadliqDbContext context) : base(context)
        {
        }
    }
}
