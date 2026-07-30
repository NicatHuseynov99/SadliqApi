using Core.DataAccess.EntityFramework;
using DataAccess.Abstract;
using DataAccess.Concrete.EntityFramework.Contexts;
using Entities.Concrete;

namespace DataAccess.Concrete
{
    public class EfAuditLogDal : EfEntityRepositoryBase<AuditLog, SadliqDbContext>, IAuditLogDal
    {
        public EfAuditLogDal(SadliqDbContext context) : base(context)
        {
        }
    }
}
