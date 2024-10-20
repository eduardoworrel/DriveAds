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
    public class IndexModel : PageModel
    {
        private readonly webapp.Data.ApplicationDbContext _context;

        public IndexModel(webapp.Data.ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<Advertisement> Advertisement { get;set; } = default!;

        public async Task OnGetAsync()
        {
            Advertisement = await _context.Advertisements.ToListAsync();
        }
    }
}
