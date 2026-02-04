using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserManagement.BackEnd.Models;
using UserManagement.BackEnd.Services;
using Vue.Common.Auth;

namespace UserManagement.BackEnd.WebApi.Controllers
{
    [Route("api/user")]
    [ApiController]
    [Authorize(Roles = "Administrator,SystemAdministrator")]
    public class UserController : ControllerBase
    {
        readonly IUserContext _userContext;
        private readonly IUserOrchestratorService _userOrchestratorService;
        private readonly ILogger<UsersController> _logger;

        public UserController(IUserContext userContext, 
            IUserOrchestratorService userOrchestratorService, 
            ILogger<UsersController> logger)
        {
            _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
            _userOrchestratorService = userOrchestratorService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<ActionResult<User>> UpdateUser([FromBody] User user)
        {
            var token = CancellationToken.None;
            var currentUser = await _userOrchestratorService.GetUserAsync(_userContext.UserCompanyShortCode, user.Id, token);
            if (currentUser == null)
            {
                return NotFound(new { error = $"User with ID {user.Id} not found" });
            }
            currentUser.Role = user.Role;
            currentUser.RoleId = user.RoleId;
            currentUser.SurveyVueEditingAvailable = user.SurveyVueEditingAvailable;
            currentUser.SurveyVueFeedbackAvailable = user.SurveyVueFeedbackAvailable;
            currentUser.FirstName = user.FirstName;
            currentUser.LastName = user.LastName;
            currentUser.Products = user.Products;
            await _userOrchestratorService.UpdateUser(currentUser, _userContext.UserCompanyShortCode, _userContext.UserId, token);
            return Ok(currentUser);
        }

        [HttpPost("AddUser")]
        public async Task<ActionResult<User>> AddUser([FromBody] UserToAdd user, CancellationToken token)
        {
            try
            {
                await _userOrchestratorService.AddUser(user, _userContext.UserCompanyShortCode, _userContext.UserId, token);
                return Ok(user);
            }
            catch (AuthServer.GeneratedAuthApi.ApiException e)
            {
                if (e.StatusCode == 500)
                {
                    return BadRequest($"Internal error {e.ToString()}");
                }
                return BadRequest(e.Response);
            }
        }

        [HttpGet("get/{userId}")]
        public async Task<ActionResult<User>> Get(string userId, CancellationToken token)
        {
            try
            {
                return Ok(await _userOrchestratorService.GetUserAsync(_userContext.AuthCompany, userId, token));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to find user {UserId}", userId);
                return BadRequest(new
                {
                    error = $"Failed to find user {userId}"
                });
            }
        }

        [HttpDelete("delete/{userId}")]
        public async Task<IActionResult> DeleteUser([FromQuery]string userEmail, string userId, CancellationToken token)
        {
            try
            {
                await _userOrchestratorService.DeleteUser(_userContext.UserCompanyShortCode,
                    _userContext.UserName, userId, token);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete user {UserId} {userEmail}", userId, userEmail);
                return BadRequest(new { error = $"Failed to delete user {userEmail}" });
            }
        }

        [HttpPost("forgotpasswordemail")]
        public async Task<IActionResult> ForgotPasswordEmail(string userEmail, CancellationToken token)
        {
            try
            {
                await _userOrchestratorService.ForgotPassword(_userContext.UserCompanyShortCode, userEmail,
                    token);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send forgot password email for {UserEmail}", userEmail);
                return BadRequest(new
                {
                    error = $"Failed to send forgot password email for {userEmail}"
                });
            }
        }
    }
}