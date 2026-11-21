
namespace TrungNguyenCafe.API.Models.Tenants
{
    public class TenantResponse
    {
        // Mã định danh chi nhánh
        public Guid TenantId { get; set; }

        // Mã chi nhánh
        public string TenantCode { get; set; } = string.Empty;

        // Tên chi nhánh
        public string TenantName { get; set; } = string.Empty;

        // Địa chỉ chi nhánh
        public string Address { get; set; } = string.Empty;

        // Trạng thái hoạt động
        public bool IsActive { get; set; }
    }
}
