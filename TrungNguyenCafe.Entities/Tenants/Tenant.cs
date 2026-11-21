
using System;

namespace TrungNguyenCafe.Entities.Tenants
{
    public class Tenant
    {
        // Khóa chính của chi nhánh
        public Guid TenantId { get; set; } = Guid.NewGuid();

        // Mã chi nhánh
        public string TenantCode { get; set; } = string.Empty;

        // Tên chi nhánh
        public string TenantName { get; set; } = string.Empty;

        // Địa chỉ chi nhánh
        public string Address { get; set; } = string.Empty;

        // Trạng thái hoạt động của chi nhánh
        public bool IsActive { get; set; } = true;
    }
}
