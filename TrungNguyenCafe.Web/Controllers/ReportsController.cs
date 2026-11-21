
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using TrungNguyenCafe.Web.Models.Orders;
using TrungNguyenCafe.Web.Models.Reports;

namespace TrungNguyenCafe.Web.Controllers
{
    [Authorize(Roles =
        "Admin,HQManager,FranchiseOwner,StoreManager")]
    public class ReportsController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public ReportsController(
            IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        // =====================================
        // 1. HTTP CLIENT KÈM JWT
        // =====================================

        private HttpClient CreateApiClient()
        {
            var client = _httpClientFactory.CreateClient("CafeApi");

            var token = HttpContext.Session.GetString("AccessToken");

            if (!string.IsNullOrWhiteSpace(token))
            {
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
            }

            return client;
        }

        // =====================================
        // 2. XỬ LÝ JWT HẾT HẠN
        // =====================================

        private async Task<IActionResult> HandleUnauthorizedAsync()
        {
            HttpContext.Session.Clear();

            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);

            TempData["Error"] =
                "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.";

            return RedirectToAction("Login", "Account");
        }

        // =====================================
        // 3. ĐỌC LỖI API
        // =====================================

        private static async Task<string> GetApiErrorAsync(
            HttpResponseMessage response,
            string fallback)
        {
            var content = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(content))
                return fallback;

            try
            {
                using var document = JsonDocument.Parse(content);

                var root = document.RootElement;

                if (root.TryGetProperty("message", out var message))
                    return message.GetString() ?? fallback;

                if (root.TryGetProperty("title", out var title))
                    return title.GetString() ?? fallback;
            }
            catch (JsonException)
            {
            }

            return fallback;
        }

        // =====================================
        // 4. TRANG BÁO CÁO DOANH THU
        // =====================================

        [HttpGet]
        public async Task<IActionResult> Index(
            DateTime? fromDate = null,
            DateTime? toDate = null,
            Guid? tenantId = null)
        {
            // Giờ Việt Nam UTC+7
            var today = DateTime.UtcNow.AddHours(7).Date;

            // Mặc định xem từ đầu tháng đến hôm nay
            var startDate = fromDate?.Date
                ?? new DateTime(today.Year, today.Month, 1);

            var endDate = toDate?.Date ?? today;

            bool isAdmin = User.IsInRole("Admin");

            // Chỉ Admin có danh sách chọn chi nhánh
            // Vì API Tenants hiện tại chỉ cấp quyền Admin
            var model = new SalesReportViewModel
            {
                FromDate = startDate,
                ToDate = endDate,
                TenantId = isAdmin ? tenantId : null,
                CanFilterTenant = isAdmin
            };

            // Kiểm tra ngày
            if (startDate > endDate)
            {
                TempData["Error"] =
                    "Ngày bắt đầu không được lớn hơn ngày kết thúc.";

                return View(model);
            }

            using var client = CreateApiClient();

            // =================================
            // 5. TẢI DANH SÁCH CHI NHÁNH
            // =================================

            if (isAdmin)
            {
                using var tenantResponse = await client.GetAsync(
                    "api/Tenants?includeInactive=false");

                if (tenantResponse.StatusCode == HttpStatusCode.Unauthorized)
                    return await HandleUnauthorizedAsync();

                if (!tenantResponse.IsSuccessStatusCode)
                {
                    TempData["Error"] =
                        await GetApiErrorAsync(
                            tenantResponse,
                            "Không thể tải danh sách chi nhánh.");
                }
                else
                {
                    model.Tenants =
                        await tenantResponse.Content
                            .ReadFromJsonAsync<List<TenantOptionViewModel>>()
                        ?? new List<TenantOptionViewModel>();
                }
            }

            // =================================
            // 6. CHUYỂN NGÀY VIỆT NAM SANG UTC
            // =================================

            var fromUtc = DateTime.SpecifyKind(
                startDate.AddHours(-7),
                DateTimeKind.Utc);

            var toUtcExclusive = DateTime.SpecifyKind(
                endDate.AddDays(1).AddHours(-7),
                DateTimeKind.Utc);

            // =================================
            // 7. GỌI API BÁO CÁO
            // =================================

            var url =
                "api/Reports/sales" +
                $"?fromUtc={Uri.EscapeDataString(fromUtc.ToString("O", CultureInfo.InvariantCulture))}" +
                $"&toUtcExclusive={Uri.EscapeDataString(toUtcExclusive.ToString("O", CultureInfo.InvariantCulture))}";

            if (model.TenantId.HasValue)
            {
                url += $"&tenantId={model.TenantId.Value}";
            }

            using var response = await client.GetAsync(url);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
                return await HandleUnauthorizedAsync();

            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = await GetApiErrorAsync(
                    response,
                    "Không thể tải báo cáo doanh thu.");

                return View(model);
            }

            // =================================
            // 8. NHẬN DỮ LIỆU TỪ API
            // =================================

            var result = await response.Content
                .ReadFromJsonAsync<SalesReportViewModel>();

            if (result == null)
            {
                TempData["Error"] =
                    "Không nhận được dữ liệu báo cáo.";

                return View(model);
            }

            // =================================
            // 9. GÁN DỮ LIỆU CHO VIEW
            // =================================

            model.TotalRevenue = result.TotalRevenue;

            model.PaidOrderCount = result.PaidOrderCount;

            model.AverageOrderValue = result.AverageOrderValue;

            model.TotalItemsSold = result.TotalItemsSold;

            model.DailyRevenue = result.DailyRevenue
                ?? new List<DailyRevenueViewModel>();

            model.TopProducts = result.TopProducts
                ?? new List<TopProductViewModel>();

            return View(model);
        }
    }
}
