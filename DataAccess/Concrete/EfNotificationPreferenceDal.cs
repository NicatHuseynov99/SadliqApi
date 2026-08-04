using Core.DataAccess.EntityFramework;
using DataAccess.Abstract;
using DataAccess.Concrete.EntityFramework.Contexts;
using Entities.Concrete;

namespace DataAccess.Concrete
{
    public class EfNotificationPreferenceDal
        : EfEntityRepositoryBase<NotificationPreference, SadliqDbContext>, INotificationPreferenceDal
    {
        public EfNotificationPreferenceDal(SadliqDbContext context) : base(context)
        {
        }
    }

    public class EfNotificationCampaignDal
        : EfEntityRepositoryBase<NotificationCampaign, SadliqDbContext>, INotificationCampaignDal
    {
        public EfNotificationCampaignDal(SadliqDbContext context) : base(context)
        {
        }
    }
}
