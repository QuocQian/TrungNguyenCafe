
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using TrungNguyenCafe.Web.Models.Tenants;

namespace TrungNguyenCafe.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class TenantsController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public TenantsController(
            IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        // =====================================
        // 1. TẠO HTTP CLIENT KÈM JWT
        // =====================================

        private HttpClient CreateApiClient()
        {
            var client = _httpClientFactory
                .CreateClient("CafeApi");

            var token = HttpContext.Session
                .GetString("AccessToken");

            if (!string.IsNullOrWhiteSpace(token))
            {
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue(
                        "Bearer", token);
            }

            return client;
        }

        // =====================================
        // 2. XỬ LÝ KHI JWT HẾT HẠN
        // =====================================

        private async Task<IActionResult>
            HandleUnauthorizedAsync()
        {
            HttpContext.Session.Clear();

            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);

            TempData["Error"] =
                "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.";

            return RedirectToAction("Login", "Account");
        }

        // =====================================
        // 3. ĐỌC THÔNG BÁO LỖI TỪ API
        // =====================================

        private static async Task<string> GetApiErrorAsync(
            HttpResponseMessage response,
            string fallback)
        {
            var content = await response.Content
                .ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(content))
                return fallback;

            try
            {
                using var document =
                    JsonDocument.Parse(content);

                var root = document.RootElement;

                if (root.TryGetProperty(
                    "message", out var message))
                {
                    return message.GetString() ?? fallback;
                }

                if (root.TryGetProperty(
                    "title", out var title))
                {
                    return title.GetString() ?? fallback;
                }
            }
            catch (JsonException)
            {
                // Sử dụng thông báo mặc định
            }

            return fallback;
        }

        // =====================================
        // 4. DANH SÁCH CHI NHÁNH
        // =====================================

        [HttpGet]
        public async Task<IActionResult> Index(
            bool includeInactive = false)
        {
            using var client = CreateApiClient();

            using var response = await client.GetAsync(
                $"api/Tenants?includeInactive={includeInactive}");

            if (response.StatusCode == HttpStatusCode.Unauthorized)
                return await HandleUnauthorizedAsync();

            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] =
                    await GetApiErrorAsync(
                        response,
                        "Không thể tải danh sách chi nhánh.");

                return View(new List<TenantViewModel>());
            }

            var tenants = await response.Content
                .ReadFromJsonAsync<List<TenantViewModel>>()
                ?? new List<TenantViewModel>();

            ViewBag.IncludeInactive = includeInactive;

            return View(tenants);
        }

        // =====================================
        // 5. HIỂN THỊ FORM THÊM CHI NHÁNH
        // =====================================

        [HttpGet]
        public IActionResult Create()
        {
            return View(new CreateTenantViewModel());
        }

        // =====================================
        // 6. THÊM CHI NHÁNH
        // =====================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            CreateTenantViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            using var client = CreateApiClient();

            using var response = await client.PostAsJsonAsync(
                "api/Tenants",
                new
                {
                    model.TenantCode,
                    model.TenantName,
                    model.Address
                });

            if (response.StatusCode == HttpStatusCode.Unauthorized)
                return await HandleUnauthorizedAsync();

            if (!response.IsSuccessStatusCode)
            {
                var error = await GetApiErrorAsync(
                    response,
                    "Không thể thêm chi nhánh.");

                if (response.StatusCode == HttpStatusCode.Conflict)
                    ModelState.AddModelError(
                        nameof(model.TenantCode), error);
                else
                    ModelState.AddModelError("", error);

                return View(model);
            }

            TempData["Success"] =
                "Thêm chi nhánh thành công!";

            return RedirectToAction(nameof(Index));
        }

        // =====================================
        // 7. HIỂN THỊ FORM CẬP NHẬT
        // =====================================

        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            using var client = CreateApiClient();

            using var response = await client.GetAsync(
                $"api/Tenants/{id}");

            if (response.StatusCode == HttpStatusCode.Unauthorized)
                return await HandleUnauthorizedAsync();

            if (response.StatusCode == HttpStatusCode.NotFound)
                return NotFound();

            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] =
                    "Không thể tải thông tin chi nhánh.";

                return RedirectToAction(nameof(Index));
            }

            var tenant = await response.Content
                .ReadFromJsonAsync<TenantViewModel>();

            if (tenant == null)
                return NotFound();

            var model = new UpdateTenantViewModel
            {
                TenantId = tenant.TenantId,
                TenantCode = tenant.TenantCode,
                TenantName = tenant.TenantName,
                Address = tenant.Address
            };

            return View(model);
        }

        // =====================================
        // 8. CẬP NHẬT CHI NHÁNH
        // =====================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            Guid id,
            UpdateTenantViewModel model)
        {
            if (id != model.TenantId)
                return BadRequest();

            if (!ModelState.IsValid)
                return View(model);

            using var client = CreateApiClient();

            using var response = await client.PutAsJsonAsync(
                $"api/Tenants/{id}",
                new
                {
                    model.TenantName,
                    model.Address
                });

            if (response.StatusCode == HttpStatusCode.Unauthorized)
                return await HandleUnauthorizedAsync();

            if (!response.IsSuccessStatusCode)
            {
                var error = await GetApiErrorAsync(
                    response,
                    "Không thể cập nhật chi nhánh.");

                ModelState.AddModelError("", error);

                return View(model);
            }

            TempData["Success"] =
                "Cập nhật chi nhánh thành công!";

            return RedirectToAction(nameof(Index));
        }

        // =====================================
        // 9. NGỪNG HOẠT ĐỘNG CHI NHÁNH
        // =====================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            using var client = CreateApiClient();

            using var response = await client.DeleteAsync(
                $"api/Tenants/{id}");

            if (response.StatusCode == HttpStatusCode.Unauthorized)
                return await HandleUnauthorizedAsync();

            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] =
                    await GetApiErrorAsync(
                        response,
                        "Không thể ngừng hoạt động chi nhánh.");

                return RedirectToAction(nameof(Index));
            }

            TempData["Success"] =
                "Đã ngừng hoạt động chi nhánh.";

            return RedirectToAction(nameof(Index));
        }

        // =====================================
        // 10. KHÔI PHỤC CHI NHÁNH
        // =====================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Restore(Guid id)
        {
            using var client = CreateApiClient();

            using var request = new HttpRequestMessage(
                HttpMethod.Patch,
                $"api/Tenants/{id}/restore");

            using var response = await client.SendAsync(request);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
                return await HandleUnauthorizedAsync();

            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] =
                    await GetApiErrorAsync(
                        response,
                        "Không thể khôi phục chi nhánh.");

                return RedirectToAction(nameof(Index),
                    new { includeInactive = true });
            }

            TempData["Success"] =
                "Khôi phục chi nhánh thành công!";

            return RedirectToAction(nameof(Index),
                new { includeInactive = true });
        }
    }
}
