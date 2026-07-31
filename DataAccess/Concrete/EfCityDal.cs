using Core.DataAccess.EntityFramework;
using DataAccess.Abstract;
using DataAccess.Concrete.EntityFramework.Contexts;
using Entities.Concrete;

namespace DataAccess.Concrete
{
    public class EfCityDal : EfEntityRepositoryBase<City, SadliqDbContext>, ICityDal
    {
        public EfCityDal(SadliqDbContext context) : base(context)
        {
        }
    }
}
