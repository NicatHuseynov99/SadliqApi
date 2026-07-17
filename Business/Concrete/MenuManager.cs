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

        public MenuManager(IMenuDal menuDal, IMenuItemDal menuItemDal)
        {
            _menuDal = menuDal;
            _menuItemDal = menuItemDal;
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

        // Menyunu və tərkibindəki yeməkləri (50/60/70 AZN paketi) birlikdə əlavə edir
        public async Task<IDataResult<Menu>> CreateAsync(CreateMenuDto dto)
        {
            var menu = new Menu
            {
                VenueId = dto.VenueId,
                Name = dto.Name,
                PricePerPerson = dto.PricePerPerson,
                Description = dto.Description
            };

            await _menuDal.AddAsync(menu);

            foreach (var item in dto.Items)
            {
                await _menuItemDal.AddAsync(new MenuItem
                {
                    MenuId = menu.Id,
                    Name = item.Name,
                    Category = item.Category,
                    Description = item.Description
                });
            }

            return new SuccessDataResult<Menu>(menu, "Menyu əlavə olundu.");
        }
    }
}
