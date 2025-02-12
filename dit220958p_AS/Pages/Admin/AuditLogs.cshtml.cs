using Microsoft.AspNetCore.Mvc.RazorPages;
using dit220958p_AS.Data;
using dit220958p_AS.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace dit220958p_AS.Pages.Admin
{
    public class AuditLogsModel : PageModel
    {
        private readonly AppDbContext _context;

        public AuditLogsModel(AppDbContext context)
        {
            _context = context;
        }

        public List<AuditLog> AuditLogs { get; set; }

        public async Task OnGetAsync()
        {
            AuditLogs = await _context.AuditLogs.OrderByDescending(a => a.Timestamp).ToListAsync();
        }
    }
}
