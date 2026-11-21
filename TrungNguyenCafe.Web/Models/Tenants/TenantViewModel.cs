
namespace TrungNguyenCafe.Web.Models.Tenants
{
    public class TenantViewModel
    {
        public Guid TenantId { get; set; }

        public string TenantCode { get; set; } = string.Empty;

        public string TenantName { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;

        public bool IsActive { get; set; }
    }
}
