
namespace TrungNguyenCafe.Entities.Security
{
    public static class RoleNames
    {
        // A1
        public const string SystemAdmin = "Admin";

        // A2
        public const string HQManager = "HQManager";

        // A3
        public const string FranchiseOwner = "FranchiseOwner";

        // A4
        public const string StoreManager = "StoreManager";

        // A5
        public const string SalesStaff = "SalesStaff";

        // Danh sách 5 vai trò chính thức
        public static readonly string[] All =
        {
            SystemAdmin,
            HQManager,
            FranchiseOwner,
            StoreManager,
            SalesStaff
        };
    }
}
