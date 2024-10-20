using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using webapp.Data;
// using webapp.ViewModels;

namespace webapp.Pages.Ad
{
    public class CreateModel : PageModel
    {
        private readonly webapp.Data.ApplicationDbContext _context;

        public CreateModel(webapp.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult OnGet()
        {
            return Page();
        }

        [BindProperty]
        public AdvertisementViewModel AdvertisementViewModel { get; set; } = default!;

        // For more information, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var advertisement = new Advertisement
            {
                Id = Guid.NewGuid(),
                Text = AdvertisementViewModel.Text,
                Ages = string.Join(",", AdvertisementViewModel.Ages),
                Pet = AdvertisementViewModel.Pet,
                Times = string.Join(",", AdvertisementViewModel.Times), 
                Where = AdvertisementViewModel.Where,
            };

            _context.Advertisements.Add(advertisement);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}