
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using TrungNguyenCafe.Web.Models.Products;
using TrungNguyenCafe.Web.Models.Tenants;

namespace TrungNguyenCafe.Web.Controllers
{
    [Authorize(Roles = "Admin,FranchiseOwner")]
    public class ProductsController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public ProductsController(
            IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        // =====================================
        // 1. HTTP CLIENT KÈM JWT
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
        // 2. XỬ LÝ JWT HẾT HẠN
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
        // 3. ĐỌC LỖI API
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
        // 4. TẢI DANH SÁCH CHI NHÁNH
        // =====================================

        private async Task LoadTenantsAsync(
            bool includeInactive = false)
        {
            using var client = CreateApiClient();

            using var response = await client.GetAsync(
                $"api/Tenants?includeInactive={includeInactive}");

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                ViewBag.Tenants = new List<TenantViewModel>();
                return;
            }

            if (response.IsSuccessStatusCode)
            {
                var tenants = await response.Content
                    .ReadFromJsonAsync<List<TenantViewModel>>()
                    ?? new List<TenantViewModel>();

                ViewBag.Tenants = tenants;
            }
            else
            {
                ViewBag.Tenants = new List<TenantViewModel>();
            }
        }

        // =====================================
        // 5. DANH SÁCH SẢN PHẨM
        // =====================================

        [HttpGet]
        public async Task<IActionResult> Index(
            bool includeInactive = false,
            Guid? tenantId = null)
        {
            bool isAdmin = User.IsInRole("Admin");

            // Lưu trạng thái bộ lọc cho giao diện
            ViewBag.IncludeInactive = includeInactive;

            ViewBag.TenantId = isAdmin
                ? tenantId
                : null;

            // Admin được xem danh sách tất cả chi nhánh
            if (isAdmin)
            {
                await LoadTenantsAsync(
                    includeInactive: true);
            }

            using var client = CreateApiClient();

            var url =
                $"api/Products?includeInactive={includeInactive}";

            // Chỉ Admin được lọc theo TenantId
            if (isAdmin && tenantId.HasValue)
            {
                url += $"&tenantId={tenantId.Value}";
            }

            using var response = await client.GetAsync(url);

            if (response.StatusCode ==
                HttpStatusCode.Unauthorized)
            {
                return await HandleUnauthorizedAsync();
            }

            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = await GetApiErrorAsync(
                    response,
                    "Không thể tải danh sách sản phẩm.");

                return View(new List<ProductViewModel>());
            }

            var products = await response.Content
                .ReadFromJsonAsync<List<ProductViewModel>>()
                ?? new List<ProductViewModel>();

            return View(products);
        }

        // =====================================
        // 6. HIỂN THỊ FORM THÊM SẢN PHẨM
        // =====================================

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new CreateProductViewModel();

            // Admin được chọn chi nhánh
            if (User.IsInRole("Admin"))
            {
                // Chỉ hiển thị chi nhánh đang hoạt động
                await LoadTenantsAsync();
            }
            else
            {
                // Chủ cửa hàng chỉ được dùng chi nhánh của mình
                var tenantId = User.FindFirst("TenantId")?.Value;

                if (Guid.TryParse(tenantId, out var id))
                {
                    model.TenantId = id;
                }
                else
                {
                    TempData["Error"] =
                        "Không xác định được chi nhánh của tài khoản.";

                    return RedirectToAction(nameof(Index));
                }
            }

            return View(model);
        }

        // =====================================
        // 7. THÊM SẢN PHẨM
        // =====================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            CreateProductViewModel model)
        {
            // Chủ cửa hàng không được tự chọn chi nhánh khác
            if (!User.IsInRole("Admin"))
            {
                var tenantId = User.FindFirst("TenantId")?.Value;

                if (!Guid.TryParse(tenantId, out var id))
                {
                    ModelState.AddModelError(
                        "",
                        "Không xác định được chi nhánh.");

                    return View(model);
                }

                model.TenantId = id;
            }

            if (!ModelState.IsValid)
            {
                if (User.IsInRole("Admin"))
                    await LoadTenantsAsync();

                return View(model);
            }

            using var client = CreateApiClient();

            using var response = await client.PostAsJsonAsync(
                "api/Products",
                new
                {
                    model.TenantId,
                    model.ProductName,
                    model.Price
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
                    "Không thể thêm sản phẩm.");

                ModelState.AddModelError("", error);

                if (User.IsInRole("Admin"))
                    await LoadTenantsAsync();

                return View(model);
            }

            TempData["Success"] =
                "Thêm sản phẩm thành công!";

            return RedirectToAction(nameof(Index));
        }

        // =====================================
        // 8. HIỂN THỊ FORM CẬP NHẬT
        // =====================================

        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            using var client = CreateApiClient();

            using var response = await client.GetAsync(
                $"api/Products/{id}");

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
                    "Không thể tải thông tin sản phẩm.";

                return RedirectToAction(nameof(Index));
            }

            var product = await response.Content
                .ReadFromJsonAsync<ProductViewModel>();

            if (product == null)
                return NotFound();

            var model = new UpdateProductViewModel
            {
                ProductId = product.ProductId,
                ProductName = product.ProductName,
                Price = product.Price
            };

            return View(model);
        }

        // =====================================
        // 9. CẬP NHẬT SẢN PHẨM
        // =====================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            Guid id,
            UpdateProductViewModel model)
        {
            if (id != model.ProductId)
                return BadRequest();

            if (!ModelState.IsValid)
                return View(model);

            using var client = CreateApiClient();

            using var response = await client.PutAsJsonAsync(
                $"api/Products/{id}",
                new
                {
                    model.ProductName,
                    model.Price
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
                    "Không thể cập nhật sản phẩm.");

                ModelState.AddModelError("", error);

                return View(model);
            }

            TempData["Success"] =
                "Cập nhật sản phẩm thành công!";

            return RedirectToAction(nameof(Index));
        }

        // =====================================
        // 10. THAY ĐỔI TRẠNG THÁI SẢN PHẨM
        // =====================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(
            Guid id,
            bool isActive,
            bool includeInactive = false,
            Guid? tenantId = null)
        {
            using var client = CreateApiClient();

            using var response = await client.PatchAsJsonAsync(
                $"api/Products/{id}/status",
                isActive);

            if (response.StatusCode ==
                HttpStatusCode.Unauthorized)
            {
                return await HandleUnauthorizedAsync();
            }

            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = await GetApiErrorAsync(
                    response,
                    "Không thể cập nhật trạng thái sản phẩm.");

                return RedirectToAction(
                    nameof(Index),
                    new
                    {
                        includeInactive = true,
                        tenantId
                    });
            }

            TempData["Success"] = isActive
                ? "Khôi phục sản phẩm thành công!"
                : "Đã ngừng kinh doanh sản phẩm.";

            return RedirectToAction(
                nameof(Index),
                new
                {
                    includeInactive = !isActive,
                    tenantId
                });
        }
    }
}
