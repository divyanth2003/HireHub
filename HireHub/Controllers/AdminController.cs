// Controllers/AdminController.cs
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using HireHub.API.Services;
using HireHub.API.Exceptions; // used for NotFoundException / ConflictException if you have them

namespace HireHub.API.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiversion}/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly UserService _userService;
        private readonly JobService _jobService;
        private readonly ApplicationService _applicationService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            UserService userService,
            JobService jobService,
            ApplicationService applicationService,
            ILogger<AdminController> logger)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _jobService = jobService ?? throw new ArgumentNullException(nameof(jobService));
            _applicationService = applicationService ?? throw new ArgumentNullException(nameof(applicationService));
            _logger = logger;
        }

        // GET: api/v1/admin/stats
        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            try
            {
                var totalUsers = await TryCountAsyncFromService(_userService);
                var totalJobs = await TryCountAsyncFromService(_jobService);
                var totalApplications = await TryCountAsyncFromService(_applicationService);

                return Ok(new
                {
                    totalUsers,
                    totalJobs,
                    totalApplications
                });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to fetch admin stats");
                return StatusCode(500, new { message = "Failed to fetch admin stats." });
            }
        }

        // GET: api/v1/admin/users
        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            try
            {
                var all = await InvokeGetAllAsync(_userService);
                return Ok(all);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to fetch users");
                return StatusCode(500, new { message = "Failed to fetch users" });
            }
        }

        // DELETE: api/v1/admin/users/{id}
        [HttpDelete("users/{id:guid}")]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            try
            {
                await _userService.DeleteAsync(id);
                return NoContent();
            }
            catch (NotFoundException nf)
            {
                _logger?.LogWarning(nf, "Delete user failed - not found: {UserId}", id);
                return NotFound(new { message = nf.Message });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to delete user {UserId}", id);
                return StatusCode(500, new { message = "Failed to delete user." });
            }
        }

        // GET: api/v1/admin/jobs
        [HttpGet("jobs")]
        public async Task<IActionResult> GetJobs()
        {
            try
            {
                var all = await InvokeGetAllAsync(_jobService);
                return Ok(all);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to fetch jobs");
                return StatusCode(500, new { message = "Failed to fetch jobs" });
            }
        }

        // DELETE: api/v1/admin/jobs/{id}
        [HttpDelete("jobs/{id:int}")]
        public async Task<IActionResult> DeleteJob(int id)
        {
            try
            {
                await _jobService.DeleteAsync(id);
                return NoContent();
            }
            catch (NotFoundException nf)
            {
                _logger?.LogWarning(nf, "Delete job failed - not found: {JobId}", id);
                return NotFound(new { message = nf.Message });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to delete job {JobId}", id);
                return StatusCode(500, new { message = "Failed to delete job." });
            }
        }

        // GET: api/v1/admin/applications
        [HttpGet("applications")]
        public async Task<IActionResult> GetApplications()
        {
            try
            {
                var all = await InvokeGetAllAsync(_applicationService);
                return Ok(all);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to fetch applications");
                return StatusCode(500, new { message = "Failed to fetch applications" });
            }
        }
        // GET api/v1/admin/users/{id}
        [HttpGet("users/{id:guid}")]
        public async Task<IActionResult> GetUser(Guid id)
        {
            try
            {
                var user = await _userService.GetByIdAsync(id);
                if (user == null) return NotFound();
                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to fetch user {UserId}", id);
                return StatusCode(500, new { message = "Failed to fetch user" });
            }
        }

        // DELETE: api/v1/admin/applications/{id}
        [HttpDelete("applications/{id:int}")]
        public async Task<IActionResult> DeleteApplication(int id)
        {
            try
            {
                await _applicationService.DeleteAsync(id);
                return NoContent();
            }
            catch (NotFoundException nf)
            {
                _logger?.LogWarning(nf, "Delete application failed - not found: {ApplicationId}", id);
                return NotFound(new { message = nf.Message });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to delete application {ApplicationId}", id);
                return StatusCode(500, new { message = "Failed to delete application." });
            }
        }

        // ---------------- helpers ----------------

        private static async Task<long> TryCountAsyncFromService(object service)
        {
            if (service == null) return 0;

            // 1) If service provides CountAsync(), call it
            var countMethod = service.GetType().GetMethod("CountAsync", Type.EmptyTypes);
            if (countMethod != null)
            {
                var task = (Task)countMethod.Invoke(service, null);
                await task.ConfigureAwait(false);
                var resultProp = task.GetType().GetProperty("Result");
                var result = resultProp?.GetValue(task);
                if (result is long l) return l;
                if (result is int i) return i;
            }

            // 2) Fallback: call GetAllAsync() and count
            var getAllMethod = service.GetType().GetMethod("GetAllAsync", Type.EmptyTypes);
            if (getAllMethod != null)
            {
                var task = (Task)getAllMethod.Invoke(service, null);
                await task.ConfigureAwait(false);
                var resultProp = task.GetType().GetProperty("Result");
                var result = resultProp?.GetValue(task);
                if (result is System.Collections.IEnumerable enumerable)
                {
                    long c = 0;
                    foreach (var _ in enumerable) c++;
                    return c;
                }
            }

            return 0;
        }

        private static async Task<object> InvokeGetAllAsync(object service)
        {
            var getAllMethod = service?.GetType().GetMethod("GetAllAsync", Type.EmptyTypes);
            if (getAllMethod == null) return Array.Empty<object>();

            var task = (Task)getAllMethod.Invoke(service, null);
            await task.ConfigureAwait(false);
            var resultProp = task.GetType().GetProperty("Result");
            return resultProp?.GetValue(task);
        }
    }
}