
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using TrungNguyenCafe.Web.Models.Dashboard;

namespace TrungNguyenCafe.Web.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public HomeController(
            IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

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

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var model = new DashboardViewModel
            {
                Today = DateTime.UtcNow.AddHours(7)
            };

            try
            {
                using var client = CreateApiClient();

                using var response = await client.GetAsync(
                    "api/Dashboard/summary");

                if (response.StatusCode ==
                    HttpStatusCode.Unauthorized)
                {
                    return await HandleUnauthorizedAsync();
                }

                if (!response.IsSuccessStatusCode)
                {
                    model.ErrorMessage =
                        $"Không thể tải dữ liệu Dashboard. " +
                        $"Mã lỗi: {(int)response.StatusCode}.";

                    return View(model);
                }

                var result = await response.Content
                    .ReadFromJsonAsync<DashboardViewModel>();

                if (result == null)
                {
                    model.ErrorMessage =
                        "Backend không trả về dữ liệu thống kê.";

                    return View(model);
                }

                result.Today = DateTime.UtcNow.AddHours(7);

                result.IsLoaded = true;

                return View(result);
            }
            catch (HttpRequestException)
            {
                model.ErrorMessage =
                    "Không thể kết nối Backend. Vui lòng kiểm tra API.";

                return View(model);
            }
        }
    }
}
