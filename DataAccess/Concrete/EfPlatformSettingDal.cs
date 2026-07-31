using Core.DataAccess.EntityFramework;
using DataAccess.Abstract;
using DataAccess.Concrete.EntityFramework.Contexts;
using Entities.Concrete;

namespace DataAccess.Concrete
{
    public class EfPlatformSettingDal : EfEntityRepositoryBase<PlatformSetting, SadliqDbContext>, IPlatformSettingDal
    {
        public EfPlatformSettingDal(SadliqDbContext context) : base(context)
        {
        }
    }
}
