using Core.DataAccess.EntityFramework;
using DataAccess.Abstract;
using DataAccess.Concrete.EntityFramework.Contexts;
using Entities.Concrete;

namespace DataAccess.Concrete
{
    public class EfFavoriteDal : EfEntityRepositoryBase<Favorite, SadliqDbContext>, IFavoriteDal
    {
        public EfFavoriteDal(SadliqDbContext context) : base(context)
        {
        }
    }
}
