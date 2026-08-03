using Business.Abstract;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;
using Entities.Dtos.Menu;
using Microsoft.EntityFrameworkCore;

namespace Business.Concrete
{
    public class MenuManager : IMenuService
    {
        private readonly IMenuDal _menuDal;
        private readonly IMenuItemDal _menuItemDal;
        private readonly IVenueDal _venueDal;
        private readonly IVenueAccessService _access;

        public MenuManager(
            IMenuDal menuDal,
            IMenuItemDal menuItemDal,
            IVenueDal venueDal,
            IVenueAccessService access)
        {
            _menuDal = menuDal;
            _menuItemDal = menuItemDal;
            _venueDal = venueDal;
            _access = access;
        }

        // Sarayın bütün menyu paketlərini (50/60/70 AZN və s.) tərkibi ilə birgə qaytarır
        public async Task<IDataResult<List<Menu>>> GetByVenueIdAsync(int venueId)
        {
            var query = await _menuDal.GetListAsync(m => m.VenueId == venueId);
            var menus = await query
                .Include(m => m.Items)
                .ToListAsync();

            return new SuccessDataResult<List<Menu>>(menus);
        }

        // Tək menyunun tərkibi ilə birgə detalları
        public async Task<IDataResult<Menu>> GetByIdAsync(int id)
        {
            var query = await _menuDal.GetListAsync(m => m.Id == id);
            var menu = await query
                .Include(m => m.Items)
                .FirstOrDefaultAsync();

            return menu is null
                ? new ErrorDataResult<Menu>("Menyu tapılmadı.")
                : new SuccessDataResult<Menu>(menu);
        }

        // Menyunu və tərkibindəki yeməkləri (50/60/70 AZN paketi) birlikdə əlavə edir
        public async Task<IDataResult<Menu>> CreateAsync(CreateMenuDto dto, int ownerId)
        {
            // Sahiblik və təsdiq yoxlaması mərkəzi servisdədir
            if (!await _access.CanModifyAsync(dto.VenueId, ownerId))
                return new ErrorDataResult<Menu>("Bu saray sizə aid deyil və ya hələ təsdiqlənməyib.");

            var menu = new Menu
            {
                VenueId = dto.VenueId,
                Name = dto.Name,
                PricePerPerson = dto.PricePerPerson,
                Description = dto.Description,
                ImageUrl = dto.ImageUrl
            };

            await _menuDal.AddAsync(menu);

            foreach (var item in dto.Items.Where(i => !string.IsNullOrWhiteSpace(i.Name)))
            {
                await _menuItemDal.AddAsync(new MenuItem
                {
                    MenuId = menu.Id,
                    Name = item.Name,
                    Category = item.Category,
                    Description = item.Description,
                    ImageUrl = item.ImageUrl
                });
            }

            return new SuccessDataResult<Menu>(menu, "Menyu əlavə olundu.");
        }

        // Restoran sahibinin öz menyusunun əsas məlumatlarını redaktə etməsi
        public async Task<IResult> UpdateAsync(int id, UpdateMenuDto dto, int ownerId)
        {
            var menu = await _menuDal.GetAsync(m => m.Id == id);
            if (menu is null)
                return new ErrorResult("Menyu tapılmadı.");

            if (!await _access.CanModifyAsync(menu.VenueId, ownerId))
                return new ErrorResult("Bu menyu sizə aid deyil və ya saray hələ təsdiqlənməyib.");

            menu.Name = dto.Name;
            menu.PricePerPerson = dto.PricePerPerson;
            menu.Description = dto.Description;

            if (dto.ImageUrl is not null)
                menu.ImageUrl = dto.ImageUrl;

            await _menuDal.UpdateAsync(menu);
            return new SuccessResult("Menyu yeniləndi.");
        }

        // Restoran sahibinin öz menyusunu silməsi
        public async Task<IResult> DeleteAsync(int id, int ownerId)
        {
            var menu = await _menuDal.GetAsync(m => m.Id == id);
            if (menu is null)
                return new ErrorResult("Menyu tapılmadı.");

            if (!await _access.CanModifyAsync(menu.VenueId, ownerId))
                return new ErrorResult("Bu menyu sizə aid deyil və ya saray hələ təsdiqlənməyib.");

            await _menuDal.DeleteAsync(menu);
            return new SuccessResult("Menyu silindi.");
        }
    }
}
