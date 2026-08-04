using Core.DataAccess.EntityFramework;
using DataAccess.Abstract;
using DataAccess.Concrete.EntityFramework.Contexts;
using Entities.Concrete;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Concrete
{
    public class EfNotificationDal : EfEntityRepositoryBase<Notification, SadliqDbContext>, INotificationDal
    {
        public EfNotificationDal(SadliqDbContext context) : base(context)
        {
        }

        public async Task AddManyAsync(IEnumerable<Notification> notifications)
        {
            await _context.Notifications.AddRangeAsync(notifications);
            await _context.SaveChangesAsync();
        }

        public async Task<int> GetUnreadCountAsync(int userId)
        {
            return await _context.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead);
        }

        public async Task<int> MarkAllReadAsync(int userId)
        {
            var now = DateTime.UtcNow;

            return await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ExecuteUpdateAsync(set => set
                    .SetProperty(n => n.IsRead, true)
                    .SetProperty(n => n.ReadAt, now));
        }

        // Oxunma və keçid sayı bildiriş sətirlərindən hesablanır — elan sətrində
        // saxlanılan rəqəmlər yalnız göstərmək üçündür
        public async Task RefreshCampaignStatsAsync(int campaignId)
        {
            var campaign = await _context.NotificationCampaigns
                .FirstOrDefaultAsync(c => c.Id == campaignId);

            if (campaign is null) return;

            var rows = _context.Notifications.Where(n => n.CampaignId == campaignId);

            campaign.RecipientCount = await rows.CountAsync();
            campaign.ReadCount = await rows.CountAsync(n => n.IsRead);
            campaign.ClickCount = await rows.CountAsync(n => n.ClickedAt != null);

            await _context.SaveChangesAsync();
        }
    }
}
