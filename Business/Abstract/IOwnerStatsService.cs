using Core.Utilities.Results;
using Entities.Dtos.Owner;

namespace Business.Abstract
{
    // Sahibkarın hesabatı: baxış, sorğu axını, cavab müddəti və məsləhətlər
    public interface IOwnerStatsService
    {
        Task<IDataResult<List<OwnerStatsDto>>> GetAsync(int ownerId, int days, int? venueId);
    }
}
