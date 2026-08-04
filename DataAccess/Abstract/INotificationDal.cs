using Core.DataAccess;
using Entities.Concrete;
using Entities.Dtos.Notification;

namespace DataAccess.Abstract
{
    public interface INotificationDal : IEntityRepository<Notification>
    {
        // Minlərlə sətri bir-bir yazmaq olmaz — toplu əlavə
        Task AddManyAsync(IEnumerable<Notification> notifications);

        Task<int> GetUnreadCountAsync(int userId);

        // Tək-tək oxumaq əvəzinə birbaşa bazada yenilənir
        Task<int> MarkAllReadAsync(int userId);

        // Elanın nəticə göstəricilərini yenidən hesablayır
        Task RefreshCampaignStatsAsync(int campaignId);
    }

    // Auditoriya sorğusu ayrıca dayanır: bildirişlə deyil, istifadəçi
    // davranışı ilə işləyir (rezerv, favorit, aktivlik).
    public interface IAudienceDal
    {
        Task<List<int>> ResolveAsync(AudienceDto audience);
        Task<List<string>> GetSampleNamesAsync(List<int> userIds, int take);
    }
}
