
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using TrungNguyenCafe.Web.Models.Orders;

namespace TrungNguyenCafe.Web.Controllers
{
    [Authorize(Roles =
        "Admin,HQManager,FranchiseOwner,StoreManager,SalesStaff")]
    public class OrdersController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public OrdersController(IHttpClientFactory httpClientFactory)
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
        // 4. MÀN HÌNH BÁN HÀNG POS
        // =====================================

        [HttpGet]
        [Authorize(Roles =
            "Admin,FranchiseOwner,StoreManager,SalesStaff")]
        public async Task<IActionResult> Index(Guid? tenantId = null)
        {
            bool isAdmin = User.IsInRole("Admin");

            var model = new PosViewModel
            {
                IsAdmin = isAdmin,
                TenantId = tenantId
            };

            using var client = CreateApiClient();

            // Admin tải danh sách chi nhánh
            if (isAdmin)
            {
                using var tenantResponse = await client.GetAsync(
                    "api/Tenants?includeInactive=false");

                if (tenantResponse.StatusCode == HttpStatusCode.Unauthorized)
                    return await HandleUnauthorizedAsync();

                if (tenantResponse.IsSuccessStatusCode)
                {
                    model.Tenants =
                        await tenantResponse.Content
                            .ReadFromJsonAsync<List<TenantOptionViewModel>>()
                        ?? new List<TenantOptionViewModel>();
                }

                // Chưa chọn chi nhánh thì chưa tải sản phẩm
                if (!tenantId.HasValue)
                    return View(model);
            }
            else
            {
                // Các tài khoản cửa hàng lấy TenantId từ JWT
                var tenantValue = User.FindFirst("TenantId")?.Value;

                if (!Guid.TryParse(tenantValue, out var currentTenantId))
                {
                    TempData["Error"] = "Không xác định được chi nhánh.";
                    return RedirectToAction("Index", "Home");
                }

                model.TenantId = currentTenantId;
            }

            // Tải sản phẩm
            var url = "api/Products?includeInactive=false";

            if (isAdmin)
            {
                url += $"&tenantId={model.TenantId}";
            }

            using var response = await client.GetAsync(url);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
                return await HandleUnauthorizedAsync();

            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = await GetApiErrorAsync(
                    response,
                    "Không thể tải danh sách sản phẩm.");

                return View(model);
            }

            model.Products =
                await response.Content
                    .ReadFromJsonAsync<List<PosProductViewModel>>()
                ?? new List<PosProductViewModel>();

            return View(model);
        }

        // =====================================
        // 5. TẠO ĐƠN HÀNG
        // =====================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles =
            "Admin,FranchiseOwner,StoreManager,SalesStaff")]
        public async Task<IActionResult> Create(
            CreateOrderViewModel model)
        {
            bool isAdmin = User.IsInRole("Admin");

            if (model.Items == null || model.Items.Count == 0)
            {
                TempData["Error"] = "Vui lòng chọn sản phẩm.";
                return RedirectToAction(nameof(Index),
                    new { tenantId = model.TenantId });
            }

            if (model.Items.Any(x =>
                x.ProductId == Guid.Empty || x.Quantity <= 0))
            {
                TempData["Error"] = "Thông tin giỏ hàng không hợp lệ.";
                return RedirectToAction(nameof(Index),
                    new { tenantId = model.TenantId });
            }

            // Admin phải chọn chi nhánh
            if (isAdmin)
            {
                if (!model.TenantId.HasValue ||
                    model.TenantId.Value == Guid.Empty)
                {
                    TempData["Error"] = "Vui lòng chọn chi nhánh.";
                    return RedirectToAction(nameof(Index));
                }
            }
            else
            {
                // Không cho phép Client tự chọn chi nhánh khác
                var tenantValue = User.FindFirst("TenantId")?.Value;

                if (!Guid.TryParse(tenantValue, out var currentTenantId))
                    return Forbid();

                model.TenantId = currentTenantId;
            }

            // Payload gửi Backend
            var payload = new
            {
                TenantId = isAdmin ? model.TenantId : null,

                Items = model.Items.Select(x => new
                {
                    x.ProductId,
                    x.Quantity
                }).ToList()
            };

            using var client = CreateApiClient();

            using var response = await client.PostAsJsonAsync(
                "api/Orders",
                payload);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
                return await HandleUnauthorizedAsync();

            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = await GetApiErrorAsync(
                    response,
                    "Không thể tạo đơn hàng.");

                return RedirectToAction(nameof(Index),
                    new { tenantId = model.TenantId });
            }

            var order = await response.Content
                .ReadFromJsonAsync<OrderViewModel>();

            if (order == null)
            {
                TempData["Error"] = "Không đọc được thông tin đơn hàng.";
                return RedirectToAction(nameof(History));
            }

            TempData["Success"] = "Tạo đơn hàng thành công!";

            return RedirectToAction(
                nameof(Details),
                new { id = order.OrderId });
        }

        // =====================================
        // 6. DANH SÁCH ĐƠN HÀNG
        // =====================================

        [HttpGet]
        public async Task<IActionResult> History(Guid? tenantId = null)
        {
            bool isAdmin = User.IsInRole("Admin");

            var model = new OrderHistoryViewModel
            {
                CanFilterTenant = isAdmin,
                TenantId = tenantId
            };

            using var client = CreateApiClient();

            if (isAdmin)
            {
                using var tenantResponse = await client.GetAsync(
                    "api/Tenants?includeInactive=false");

                if (tenantResponse.StatusCode == HttpStatusCode.Unauthorized)
                    return await HandleUnauthorizedAsync();

                if (tenantResponse.IsSuccessStatusCode)
                {
                    model.Tenants =
                        await tenantResponse.Content
                            .ReadFromJsonAsync<List<TenantOptionViewModel>>()
                        ?? new List<TenantOptionViewModel>();
                }
            }

            var url = "api/Orders";

            if (isAdmin && tenantId.HasValue)
            {
                url += $"?tenantId={tenantId.Value}";
            }

            using var response = await client.GetAsync(url);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
                return await HandleUnauthorizedAsync();

            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = await GetApiErrorAsync(
                    response,
                    "Không thể tải danh sách đơn hàng.");

                return View(model);
            }

            model.Orders =
                await response.Content
                    .ReadFromJsonAsync<List<OrderViewModel>>()
                ?? new List<OrderViewModel>();

            return View(model);
        }

        // =====================================
        // 7. CHI TIẾT ĐƠN HÀNG
        // =====================================

        [HttpGet]
        public async Task<IActionResult> Details(Guid id)
        {
            using var client = CreateApiClient();

            using var response = await client.GetAsync(
                $"api/Orders/{id}");

            if (response.StatusCode == HttpStatusCode.Unauthorized)
                return await HandleUnauthorizedAsync();

            if (response.StatusCode == HttpStatusCode.NotFound)
                return NotFound();

            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = await GetApiErrorAsync(
                    response,
                    "Không thể tải chi tiết đơn hàng.");

                return RedirectToAction(nameof(History));
            }

            var order = await response.Content
                .ReadFromJsonAsync<OrderViewModel>();

            if (order == null)
                return NotFound();

            return View(order);
        }

        // =====================================
        // 8. THANH TOÁN TIỀN MẶT
        // =====================================


        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,FranchiseOwner,StoreManager,SalesStaff")]
        public async Task<IActionResult> PayCash(
            Guid id,
            bool returnToHistory = false,
            Guid? tenantId = null)
        {
            using var client = CreateApiClient();

            using var request = new HttpRequestMessage(
                HttpMethod.Patch,
                $"api/Orders/{id}/cash-payment");

            using var response = await client.SendAsync(request);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
                return await HandleUnauthorizedAsync();

            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = await GetApiErrorAsync(
                    response,
                    "Không thể ghi nhận thanh toán.");

                if (returnToHistory)
                    return RedirectToAction(
                        nameof(History),
                        new { tenantId });

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            TempData["Success"] =
                "Thanh toán thành công! Đơn hàng đã được ghi nhận doanh thu.";

            if (returnToHistory)
                return RedirectToAction(
                    nameof(History),
                    new { tenantId });

            return RedirectToAction(
                nameof(Details),
                new { id });
        }

    }
}
