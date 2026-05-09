using Microsoft.AspNetCore.Identity;

namespace CMMS.Domain.Entities;

public class ApplicationUser : IdentityUser<Guid>
{
    public string FullName { get; set; } = string.Empty;
}
