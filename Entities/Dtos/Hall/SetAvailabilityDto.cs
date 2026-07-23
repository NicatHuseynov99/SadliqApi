using Entities.Concrete;

namespace Entities.Dtos.Hall
{
    public class SetAvailabilityDto
    {
        public DateOnly Date { get; set; }
        public AvailabilityStatus Status { get; set; }
    }
}
