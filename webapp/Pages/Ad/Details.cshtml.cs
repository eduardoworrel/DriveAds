using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using webapp.Data;

namespace webapp.Pages.Ad
{
    public class DetailsModel : PageModel
    {
        private readonly webapp.Data.ApplicationDbContext _context;

        public DetailsModel(webapp.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        public Advertisement Advertisement { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var advertisement = await _context.Advertisements.FirstOrDefaultAsync(m => m.Id == id);
            if (advertisement == null)
            {
                return NotFound();
            }
            else
            {
                Advertisement = advertisement;
            }
            return Page();
        }
    }
}
