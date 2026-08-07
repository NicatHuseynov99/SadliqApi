using Core.Utilities.Results;
using Entities.Dtos.Admin;
using Entities.Dtos.Common;

namespace Business.Abstract
{
    // Platforma admininin yazışmalara nəzarəti. Yalnız oxumaq üçündür —
    // admin nə yaza, nə də silə bilir, çünki yazışma mübahisədə dəlildir.
    public interface IAdminChatService
    {
        Task<IDataResult<PagedResult<AdminChatSummaryDto>>> SearchAsync(AdminChatQueryDto query);

        Task<IDataResult<AdminChatDetailDto>> GetAsync(int offerId);
    }
}
