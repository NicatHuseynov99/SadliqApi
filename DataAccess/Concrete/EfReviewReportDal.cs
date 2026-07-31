using Core.DataAccess.EntityFramework;
using DataAccess.Abstract;
using DataAccess.Concrete.EntityFramework.Contexts;
using Entities.Concrete;

namespace DataAccess.Concrete
{
    public class EfReviewReportDal : EfEntityRepositoryBase<ReviewReport, SadliqDbContext>, IReviewReportDal
    {
        public EfReviewReportDal(SadliqDbContext context) : base(context)
        {
        }
    }
}
