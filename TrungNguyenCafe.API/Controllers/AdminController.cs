using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace TrungNguyenCafe.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        [HttpGet("me")]
        public IActionResult GetAdminInfo()
        {
            return Ok(new
            {
                Message = "Xác thực Admin thành công!",

                UserName = User.Identity?.Name,

                Role = User.FindFirst(ClaimTypes.Role)?.Value,

                TenantId = User.FindFirst("TenantId")?.Value,

                IsAuthenticated = User.Identity?.IsAuthenticated
            });
        }
    }
}