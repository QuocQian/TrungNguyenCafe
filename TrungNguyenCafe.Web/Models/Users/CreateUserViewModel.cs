
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace TrungNguyenCafe.Web.Models.Users
{
    public class CreateUserViewModel : IValidatableObject
    {
        // Tên đăng nhập
        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập.")]
        [StringLength(
            100,
            ErrorMessage = "Tên đăng nhập tối đa 100 ký tự.")]
        public string UserName { get; set; } = string.Empty;

        // Mật khẩu
        [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
        [StringLength(
            100,
            MinimumLength = 8,
            ErrorMessage = "Mật khẩu phải từ 8 đến 100 ký tự.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        // Vai trò tài khoản
        [Required(ErrorMessage = "Vui lòng chọn vai trò.")]
        public string RoleName { get; set; } = string.Empty;

        // Chi nhánh được phân công
        // Null đối với tài khoản cấp hệ thống
        public Guid? TenantId { get; set; }

        // Kiểm tra tính hợp lệ của vai trò và chi nhánh
        public IEnumerable<ValidationResult> Validate(
            ValidationContext validationContext)
        {
            string[] systemRoles =
            {
                "Admin",
                "HQManager"
            };

            string[] tenantRoles =
            {
                "FranchiseOwner",
                "StoreManager",
                "SalesStaff"
            };

            if (!systemRoles.Contains(
                RoleName,
                StringComparer.OrdinalIgnoreCase)
                &&
                !tenantRoles.Contains(
                    RoleName,
                    StringComparer.OrdinalIgnoreCase))
            {
                yield return new ValidationResult(
                    "Vai trò không hợp lệ.",
                    new[] { nameof(RoleName) });

                yield break;
            }

            // Tài khoản cấp hệ thống không được gán chi nhánh
            if (systemRoles.Contains(
                RoleName,
                StringComparer.OrdinalIgnoreCase)
                && TenantId.HasValue)
            {
                yield return new ValidationResult(
                    "Tài khoản cấp hệ thống không được gán chi nhánh.",
                    new[] { nameof(TenantId) });
            }

            // Tài khoản chi nhánh bắt buộc phải có TenantId
            if (tenantRoles.Contains(
                RoleName,
                StringComparer.OrdinalIgnoreCase)
                && !TenantId.HasValue)
            {
                yield return new ValidationResult(
                    "Vui lòng chọn chi nhánh cho tài khoản.",
                    new[] { nameof(TenantId) });
            }
        }
    }
}
