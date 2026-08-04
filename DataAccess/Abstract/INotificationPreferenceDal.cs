using Core.DataAccess;
using Entities.Concrete;

namespace DataAccess.Abstract
{
    public interface INotificationPreferenceDal : IEntityRepository<NotificationPreference>
    {
    }

    public interface INotificationCampaignDal : IEntityRepository<NotificationCampaign>
    {
    }
}
