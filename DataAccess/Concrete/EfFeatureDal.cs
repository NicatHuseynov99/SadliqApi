using Core.DataAccess.EntityFramework;
using DataAccess.Abstract;
using DataAccess.Concrete.EntityFramework.Contexts;
using Entities.Concrete;

namespace DataAccess.Concrete
{
    public class EfFeatureDal : EfEntityRepositoryBase<Feature, SadliqDbContext>, IFeatureDal
    {
        public EfFeatureDal(SadliqDbContext context) : base(context)
        {
        }
    }
}
