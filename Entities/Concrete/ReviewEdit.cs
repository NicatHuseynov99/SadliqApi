using Core.Entities;

namespace Entities.Concrete
{
    // Rəyin admin tərəfindən düzəldilməsi. Əvvəlki və yeni hal saxlanılır ki,
    // platforma sahibi kimin nəyi dəyişdiyini sonradan yoxlaya bilsin.
    // Bu qeydləri yalnız SuperAdmin görür — dəstək admini öz düzəlişini gizlədə bilməsin.
    public class ReviewEdit : IEntity
    {
        public int Id { get; set; }

        public int ReviewId { get; set; }
        public Review? Review { get; set; }

        public int EditedByUserId { get; set; }
        public User? EditedByUser { get; set; }

        // Düzəliş anındakı ad və rol — hesab sonradan dəyişsə də qeyd oxunaqlı qalsın
        public string EditedByName { get; set; } = string.Empty;
        public string EditedByRole { get; set; } = string.Empty;

        public int OldRating { get; set; }
        public int NewRating { get; set; }
        public string OldComment { get; set; } = string.Empty;
        public string NewComment { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
