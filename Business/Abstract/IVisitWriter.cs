using Entities.Concrete;

namespace Business.Abstract
{
    // Ziyarət yazısı səhifənin açılmasını gecikdirməsin deyə növbəyə atılır.
    // Növbənin özü API qatındadır, biznes qatı yalnız bu müqaviləni tanıyır.
    public interface IVisitWriter
    {
        void Enqueue(PageView view);
    }
}
