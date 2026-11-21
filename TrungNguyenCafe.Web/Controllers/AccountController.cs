
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using TrungNguyenCafe.Web.Models.Auth;

namespace TrungNguyenCafe.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public AccountController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        // GET: Account/Login
        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewData["ReturnUrl"] = returnUrl;

            return View(new LoginViewModel());
        }

        // POST: Account/Login
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(
            LoginViewModel model,
            string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var client = _httpClientFactory
                    .CreateClient("CafeApi");


                var response = await client.PostAsJsonAsync(
                    "api/Auth/login",
                    new
                    {
                        UserName = model.UserName.Trim(),
                        Password = model.Password,
                        TenantCode = string.IsNullOrWhiteSpace(model.TenantCode)
                            ? null
                            : model.TenantCode.Trim()
                    });


                if (!response.IsSuccessStatusCode)
                {
                    var errorContent =
                        await response.Content.ReadAsStringAsync();

                    var errorMessage =
                        "Tên đăng nhập hoặc mật khẩu không đúng.";

                    try
                    {
                        using var document =
                            JsonDocument.Parse(errorContent);

                        if (document.RootElement.TryGetProperty(
                            "message", out var message))
                        {
                            errorMessage =
                                message.GetString() ?? errorMessage;
                        }
                    }
                    catch (JsonException)
                    {
                        // Giữ thông báo mặc định
                    }

                    ModelState.AddModelError(
                        string.Empty,
                        errorMessage);

                    return View(model);
                }

                var result = await response.Content
                    .ReadFromJsonAsync<LoginApiResponse>();

                if (result == null ||
                    string.IsNullOrWhiteSpace(result.AccessToken) ||
                    result.User == null)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "Dữ liệu đăng nhập từ máy chủ không hợp lệ.");

                    return View(model);
                }

                // 1. Lưu JWT vào Session
                HttpContext.Session.SetString(
                    "AccessToken",
                    result.AccessToken);

                // 2. Tạo Claims
                var claims = new List<Claim>
                {
                    new Claim(
                        ClaimTypes.NameIdentifier,
                        result.User.UserId.ToString()),

                    new Claim(
                        ClaimTypes.Name,
                        result.User.UserName),

                    new Claim(
                        ClaimTypes.Role,
                        result.User.RoleName)
                };

                if (result.User.TenantId.HasValue)
                {
                    claims.Add(new Claim(
                        "TenantId",
                        result.User.TenantId.Value.ToString()));
                }

                // 3. Tạo Identity
                var identity = new ClaimsIdentity(
                    claims,
                    CookieAuthenticationDefaults.AuthenticationScheme);

                var principal = new ClaimsPrincipal(identity);

                // 4. Đăng nhập bằng Cookie
                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    principal,
                    new AuthenticationProperties
                    {
                        IsPersistent = false,
                        AllowRefresh = true,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
                    });

                // 5. Chuyển về trang quản trị
                if (!string.IsNullOrEmpty(returnUrl) &&
                    Url.IsLocalUrl(returnUrl))
                {
                    return LocalRedirect(returnUrl);
                }

                return RedirectToAction("Index", "Home");
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Không thể kết nối Backend API. Vui lòng kiểm tra máy chủ.");

                return View(model);
            }
            catch (JsonException)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Không thể đọc dữ liệu phản hồi từ máy chủ.");

                return View(model);
            }
        }

        // POST: Account/Logout
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            HttpContext.Session.Clear();

            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);

            return RedirectToAction(nameof(Login));
        }

        // GET: Account/AccessDenied
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
