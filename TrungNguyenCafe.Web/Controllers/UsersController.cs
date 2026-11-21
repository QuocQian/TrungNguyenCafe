
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using TrungNguyenCafe.Web.Models.Tenants;
using TrungNguyenCafe.Web.Models.Users;

namespace TrungNguyenCafe.Web.Controllers
{
    [Authorize(Roles = "Admin,FranchiseOwner")]
    public class UsersController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public UsersController(
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
        // 4. LẤY DANH SÁCH CHI NHÁNH ĐANG HOẠT ĐỘNG
        // =====================================

        private async Task<bool> LoadTenantOptionsAsync(
            HttpClient client)
        {
            using var response = await client.GetAsync(
                "api/Tenants");

            if (response.StatusCode ==
                HttpStatusCode.Unauthorized)
            {
                return false;
            }

            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Tenants =
                    new List<TenantViewModel>();

                TempData["Error"] =
                    await GetApiErrorAsync(
                        response,
                        "Không thể tải danh sách chi nhánh.");

                return true;
            }

            ViewBag.Tenants =
                await response.Content
                    .ReadFromJsonAsync<List<TenantViewModel>>()
                ?? new List<TenantViewModel>();

            return true;
        }

        // =====================================
        // 5. KIỂM TRA 2 VAI TRÒ ĐƯỢC PHÉP GÁN
        // =====================================

        private static bool IsManagedRole(string? roleName)
        {
            return string.Equals(
                       roleName?.Trim(),
                       "FranchiseOwner",
                       StringComparison.OrdinalIgnoreCase)
                   ||
                   string.Equals(
                       roleName?.Trim(),
                       "SalesStaff",
                       StringComparison.OrdinalIgnoreCase);
        }

        // =====================================
        // 6. DANH SÁCH NHÂN VIÊN
        // =====================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            using var client = CreateApiClient();

            using var response = await client.GetAsync(
                "api/Users");

            if (response.StatusCode ==
                HttpStatusCode.Unauthorized)
            {
                return await HandleUnauthorizedAsync();
            }

            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] =
                    await GetApiErrorAsync(
                        response,
                        "Không thể tải danh sách nhân viên.");

                return View(new List<UserViewModel>());
            }

            var users = await response.Content
                .ReadFromJsonAsync<List<UserViewModel>>()
                ?? new List<UserViewModel>();

            return View(users);
        }

        // =====================================
        // 7. HIỂN THỊ FORM THÊM NHÂN VIÊN
        // =====================================

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            using var client = CreateApiClient();

            if (!await LoadTenantOptionsAsync(client))
            {
                return await HandleUnauthorizedAsync();
            }

            return View(new CreateUserViewModel());
        }

        // =====================================
        // 8. THÊM TÀI KHOẢN NHÂN VIÊN
        // =====================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            CreateUserViewModel model)
        {
            // Chỉ cho phép 2 vai trò được quản lý
            if (!IsManagedRole(model.RoleName))
            {
                ModelState.AddModelError(
                    nameof(model.RoleName),
                    "Chỉ được chọn Chủ cửa hàng hoặc Nhân viên.");
            }

            if (!ModelState.IsValid)
            {
                using var apiClient = CreateApiClient();

                if (!await LoadTenantOptionsAsync(apiClient))
                {
                    return await HandleUnauthorizedAsync();
                }

                return View(model);
            }

            using var client = CreateApiClient();

            using var response = await client.PostAsJsonAsync(
                "api/Users",
                new
                {
                    model.UserName,
                    model.Password,
                    model.RoleName,
                    model.TenantId
                });

            if (response.StatusCode ==
                HttpStatusCode.Unauthorized)
            {
                return await HandleUnauthorizedAsync();
            }

            if (!response.IsSuccessStatusCode)
            {
                var error = await GetApiErrorAsync(
                    response,
                    "Không thể tạo tài khoản nhân viên.");

                ModelState.AddModelError("", error);

                if (!await LoadTenantOptionsAsync(client))
                {
                    return await HandleUnauthorizedAsync();
                }

                return View(model);
            }

            TempData["Success"] =
                "Tạo tài khoản nhân viên thành công!";

            return RedirectToAction(nameof(Index));
        }

        // =====================================
        // 9. HIỂN THỊ FORM CHỈNH SỬA NHÂN VIÊN
        // =====================================

        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            using var client = CreateApiClient();

            using var response = await client.GetAsync(
                $"api/Users/{id}");

            if (response.StatusCode ==
                HttpStatusCode.Unauthorized)
            {
                return await HandleUnauthorizedAsync();
            }

            if (response.StatusCode ==
                HttpStatusCode.NotFound)
            {
                return NotFound();
            }

            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] =
                    "Không thể tải thông tin nhân viên.";

                return RedirectToAction(nameof(Index));
            }

            var user = await response.Content
                .ReadFromJsonAsync<UserViewModel>();

            if (user == null)
                return NotFound();

            // Không cho chỉnh sửa Admin hệ thống
            if (user.RoleName == "Admin")
            {
                TempData["Error"] =
                    "Không thể chỉnh sửa tài khoản Admin hệ thống.";

                return RedirectToAction(nameof(Index));
            }

            var model = new UpdateUserViewModel
            {
                RoleName = user.RoleName,
                TenantId = user.TenantId
            };

            ViewBag.UserName = user.UserName;
            ViewBag.UserId = user.UserId;

            if (!await LoadTenantOptionsAsync(client))
            {
                return await HandleUnauthorizedAsync();
            }

            return View(model);
        }

        // =====================================
        // 10. CẬP NHẬT TÀI KHOẢN NHÂN VIÊN
        // =====================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            Guid id,
            UpdateUserViewModel model)
        {
            if (!IsManagedRole(model.RoleName))
            {
                ModelState.AddModelError(
                    nameof(model.RoleName),
                    "Chỉ được chọn Chủ cửa hàng hoặc Nhân viên.");
            }

            ViewBag.UserId = id;

            if (!ModelState.IsValid)
            {
                using var apiClient = CreateApiClient();

                if (!await LoadTenantOptionsAsync(apiClient))
                {
                    return await HandleUnauthorizedAsync();
                }

                return View(model);
            }

            using var client = CreateApiClient();

            using var response = await client.PutAsJsonAsync(
                $"api/Users/{id}",
                new
                {
                    model.RoleName,
                    model.TenantId
                });

            if (response.StatusCode ==
                HttpStatusCode.Unauthorized)
            {
                return await HandleUnauthorizedAsync();
            }

            if (!response.IsSuccessStatusCode)
            {
                var error = await GetApiErrorAsync(
                    response,
                    "Không thể cập nhật tài khoản.");

                ModelState.AddModelError("", error);

                if (!await LoadTenantOptionsAsync(client))
                {
                    return await HandleUnauthorizedAsync();
                }

                return View(model);
            }

            TempData["Success"] =
                "Cập nhật tài khoản thành công!";

            return RedirectToAction(nameof(Index));
        }

        // =====================================
        // 11. KHÓA TÀI KHOẢN
        // =====================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Lock(Guid id)
        {
            using var client = CreateApiClient();

            using var request = new HttpRequestMessage(
                HttpMethod.Patch,
                $"api/Users/{id}/lock");

            using var response = await client.SendAsync(request);

            if (response.StatusCode ==
                HttpStatusCode.Unauthorized)
            {
                return await HandleUnauthorizedAsync();
            }

            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] =
                    await GetApiErrorAsync(
                        response,
                        "Không thể khóa tài khoản.");

                return RedirectToAction(nameof(Index));
            }

            TempData["Success"] =
                "Khóa tài khoản thành công!";

            return RedirectToAction(nameof(Index));
        }

        // =====================================
        // 12. MỞ KHÓA TÀI KHOẢN
        // =====================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unlock(Guid id)
        {
            using var client = CreateApiClient();

            using var request = new HttpRequestMessage(
                HttpMethod.Patch,
                $"api/Users/{id}/unlock");

            using var response = await client.SendAsync(request);

            if (response.StatusCode ==
                HttpStatusCode.Unauthorized)
            {
                return await HandleUnauthorizedAsync();
            }

            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] =
                    await GetApiErrorAsync(
                        response,
                        "Không thể mở khóa tài khoản.");

                return RedirectToAction(nameof(Index));
            }

            TempData["Success"] =
                "Mở khóa tài khoản thành công!";

            return RedirectToAction(nameof(Index));
        }
    }
}
