using Core.DataAccess.EntityFramework;
using DataAccess.Abstract;
using DataAccess.Concrete.EntityFramework.Contexts;
using Entities.Concrete;

namespace DataAccess.Concrete
{
    public class EfReviewDal : EfEntityRepositoryBase<Review, SadliqDbContext>, IReviewDal
    {
        public EfReviewDal(SadliqDbContext context) : base(context)
        {
        }
    }
}
