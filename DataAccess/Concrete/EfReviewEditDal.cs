using Core.DataAccess.EntityFramework;
using DataAccess.Abstract;
using DataAccess.Concrete.EntityFramework.Contexts;
using Entities.Concrete;

namespace DataAccess.Concrete
{
    public class EfReviewEditDal : EfEntityRepositoryBase<ReviewEdit, SadliqDbContext>, IReviewEditDal
    {
        public EfReviewEditDal(SadliqDbContext context) : base(context)
        {
        }
    }
}
