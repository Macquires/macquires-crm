using Application.Features.SecurityManager.Commands;
using Application.Features.SecurityManager.Queries;
using Application.Features.TelecomBackOfficeManager.Queries;
using ASPNET.BackEnd.Common.Base;
using ASPNET.BackEnd.Common.Models;
using ASPNET.BackEnd.Common.Attributes;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ASPNET.BackEnd.Controllers;


[Route("api/[controller]")]
public class SecurityController : BaseApiController
{
    private readonly IConfiguration _configuration;
    public SecurityController(ISender sender, IConfiguration configuration) : base(sender)
    {
        _configuration = configuration;
    }

    [AllowAnonymous]
    [HttpPost("Login")]
    public async Task<ActionResult<ApiSuccessResult<LoginResult>>> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<LoginResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(LoginAsync)}",
            Content = response
        });
    }

    [AllowAnonymous]
    [HttpPost("Logout")]
    public async Task<ActionResult<ApiSuccessResult<LogoutResult>>> LogoutAsync(
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(new LogoutRequest(), cancellationToken);

        return Ok(new ApiSuccessResult<LogoutResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(LogoutAsync)}",
            Content = response
        });
    }

    [AllowAnonymous]
    [HttpPost("Register")]
    public async Task<ActionResult<ApiSuccessResult<RegisterResult>>> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken
        )
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<RegisterResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(RegisterAsync)}",
            Content = response
        });
    }

    [AllowAnonymous]
    [HttpGet("ConfirmEmail")]
    public async Task<ActionResult<ApiSuccessResult<ConfirmEmailResult>>> ConfirmEmailAsync(
        [FromQuery] string email,
        [FromQuery] string code,
        CancellationToken cancellationToken
        )
    {
        var request = new ConfirmEmailRequest { Email = email, Code = code };
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<ConfirmEmailResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(ConfirmEmailAsync)}",
            Content = response
        });
    }


    [AllowAnonymous]
    [HttpPost("ForgotPassword")]
    public async Task<ActionResult<ApiSuccessResult<ForgotPasswordResult>>> ForgotPasswordAsync(
        ForgotPasswordRequest request,
        CancellationToken cancellationToken
        )
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<ForgotPasswordResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(ForgotPasswordAsync)}",
            Content = response
        });
    }

    [AllowAnonymous]
    [HttpGet("ForgotPasswordConfirmation")]
    public async Task<ActionResult<ApiSuccessResult<ForgotPasswordConfirmationResult>>> ForgotPasswordConfirmationAsync(
        [FromQuery] string email,
        [FromQuery] string code,
        [FromQuery] string tempPassword,
        CancellationToken cancellationToken)
    {
        var request = new ForgotPasswordConfirmationRequest { Email = email, TempPassword = tempPassword, Code = code };
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<ForgotPasswordConfirmationResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(ForgotPasswordConfirmationAsync)}",
            Content = response
        });
    }

    [AllowAnonymous]
    [HttpPost("RefreshToken")]
    public async Task<ActionResult<ApiSuccessResult<RefreshTokenResult>>> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<RefreshTokenResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(RefreshTokenAsync)}",
            Content = response
        });
    }

    [RequireAuthenticatedOperator]
    [HttpPost("ValidateToken")]
    public async Task<ActionResult<ApiSuccessResult<ValidateTokenResult>>> ValidateTokenAsync(
        ValidateTokenRequest request,
        CancellationToken cancellationToken
        )
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<ValidateTokenResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(ValidateTokenAsync)}",
            Content = response
        });
    }

    /// <summary>Authoritative permissions, landing path, and menu for the current operator (fixes stale localStorage).</summary>
    [RequireAuthenticatedOperator]
    [HttpGet("GetOperatorSession")]
    public async Task<ActionResult<ApiSuccessResult<GetOperatorSessionResult>>> GetOperatorSessionAsync(
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).Where(v => !string.IsNullOrWhiteSpace(v)).ToList();
        var response = await _sender.Send(
            new GetOperatorSessionRequest { UserId = userId, Roles = roles },
            cancellationToken);

        return Ok(new ApiSuccessResult<GetOperatorSessionResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(GetOperatorSessionAsync),
            Content = response,
        });
    }

    [RequireAuthenticatedOperator]
    [HttpGet("GetMyProfileList")]
    public async Task<ActionResult<ApiSuccessResult<GetMyProfileListResult>>> GetMyProfileListAsync(
        CancellationToken cancellationToken)
    {
        var request = new GetMyProfileListRequest();
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<GetMyProfileListResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetMyProfileListAsync)}",
            Content = response
        });
    }

    [RequireAuthenticatedOperator]
    [HttpPost("UpdateMyProfile")]
    public async Task<ActionResult<ApiSuccessResult<UpdateMyProfileResult>>> UpdateMyProfileAsync(
        UpdateMyProfileRequest request,
        CancellationToken cancellationToken
        )
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<UpdateMyProfileResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(UpdateMyProfileAsync)}",
            Content = response
        });
    }

    [RequireAuthenticatedOperator]
    [HttpPost("UpdateMyProfilePassword")]
    public async Task<ActionResult<ApiSuccessResult<UpdateMyProfilePasswordResult>>> UpdateMyProfilePasswordAsync(
        UpdateMyProfilePasswordRequest request,
        CancellationToken cancellationToken
        )
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<UpdateMyProfilePasswordResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(UpdateMyProfilePasswordAsync)}",
            Content = response
        });
    }


    [RequireAdminRolesManage]
    [HttpGet("GetRoleList")]
    public async Task<ActionResult<ApiSuccessResult<GetRoleListResult>>> GetRoleListAsync(
        CancellationToken cancellationToken
        )
    {
        var request = new GetRoleListRequest { };
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<GetRoleListResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetRoleListAsync)}",
            Content = response
        });
    }


    [RequireAdminUsersManage]
    [HttpGet("GetUserList")]
    public async Task<ActionResult<ApiSuccessResult<GetUserListResult>>> GetUserListAsync(
        CancellationToken cancellationToken
        )
    {
        var request = new GetUserListRequest();
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<GetUserListResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetUserListAsync)}",
            Content = response
        });
    }

    [RequireAdminUsersManage]
    [HttpPost("CreateUser")]
    public async Task<ActionResult<ApiSuccessResult<CreateUserResult>>> CreateUserAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken
        )
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<CreateUserResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(CreateUserAsync)}",
            Content = response
        });
    }


    [RequireAdminUsersManage]
    [HttpPost("UpdateUser")]
    public async Task<ActionResult<ApiSuccessResult<UpdateUserResult>>> UpdateUserAsync(
        UpdateUserRequest request,
        CancellationToken cancellationToken
        )
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<UpdateUserResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(UpdateUserAsync)}",
            Content = response
        });
    }

    [RequireAdminUsersManage]
    [HttpPost("DeleteUser")]
    public async Task<ActionResult<ApiSuccessResult<DeleteUserResult>>> DeleteUserAsync(
    DeleteUserRequest request,
    CancellationToken cancellationToken
    )
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<DeleteUserResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(DeleteUserAsync)}",
            Content = response
        });
    }


    [RequireAdminUsersManage]
    [HttpPost("UpdatePasswordUser")]
    public async Task<ActionResult<ApiSuccessResult<UpdatePasswordUserResult>>> UpdatePasswordUserAsync(
    UpdatePasswordUserRequest request,
    CancellationToken cancellationToken
    )
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<UpdatePasswordUserResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(UpdatePasswordUserAsync)}",
            Content = response
        });
    }

    [RequireAdminUsersManage]
    [HttpPost("GetUserRoles")]
    public async Task<ActionResult<ApiSuccessResult<GetUserRolesResult>>> GetUserRolesAsync(GetUserRolesRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<GetUserRolesResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetUserRolesAsync)}",
            Content = response
        });
    }

    [RequireAdminUsersManage]
    [HttpPost("UpdateUserRole")]
    public async Task<ActionResult<ApiSuccessResult<UpdateUserRoleResult>>> UpdateUserRoleAsync(UpdateUserRoleRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<UpdateUserRoleResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(UpdateUserRoleAsync)}",
            Content = response
        });
    }

    [RequireAuthenticatedOperator]
    [HttpPost("UpdateMyProfileAvatar")]
    public async Task<ActionResult<ApiSuccessResult<UpdateMyProfileAvatarResult>>> UpdateMyProfileAvatarAsync(UpdateMyProfileAvatarRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<UpdateMyProfileAvatarResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(UpdateMyProfileAvatarAsync)}",
            Content = response
        });
    }

    [RequireAuthenticatedOperator]
    [HttpGet("GetMenuBadges")]
    public async Task<ActionResult<ApiSuccessResult<GetMenuBadgesResult>>> GetMenuBadgesAsync(CancellationToken cancellationToken)
    {
        var response = await _sender.Send(new GetMenuBadgesRequest(), cancellationToken);
        return Ok(new ApiSuccessResult<GetMenuBadgesResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(GetMenuBadgesAsync),
            Content = response,
        });
    }

    [RequireAdminRolesManage]
    [HttpGet("GetPermissionCatalog")]
    public async Task<ActionResult<ApiSuccessResult<GetPermissionCatalogResult>>> GetPermissionCatalogAsync(
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(new GetPermissionCatalogRequest(), cancellationToken);
        return Ok(new ApiSuccessResult<GetPermissionCatalogResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(GetPermissionCatalogAsync),
            Content = response,
        });
    }

    [RequireAdminRolesManage]
    [HttpGet("GetRolePermissions")]
    public async Task<ActionResult<ApiSuccessResult<GetRolePermissionsResult>>> GetRolePermissionsAsync(
        [FromQuery] string? roleName,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(roleName))
        {
            return BadRequest(new ApiErrorResult
            {
                Code = StatusCodes.Status400BadRequest,
                Message = "roleName query parameter is required.",
            });
        }

        var response = await _sender.Send(new GetRolePermissionsRequest { RoleName = roleName.Trim() }, cancellationToken);
        return Ok(new ApiSuccessResult<GetRolePermissionsResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(GetRolePermissionsAsync),
            Content = response,
        });
    }

    [RequireAdminRolesManage]
    [HttpPost("CloneRolePermissions")]
    public async Task<ActionResult<ApiSuccessResult<CloneRolePermissionsResult>>> CloneRolePermissionsAsync(
        CloneRolePermissionsRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);
        return Ok(new ApiSuccessResult<CloneRolePermissionsResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(CloneRolePermissionsAsync),
            Content = response,
        });
    }

    [RequireAdminRolesManage]
    [HttpPost("UpdateRolePermissions")]
    public async Task<ActionResult<ApiSuccessResult<UpdateRolePermissionsResult>>> UpdateRolePermissionsAsync(
        UpdateRolePermissionsRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);
        return Ok(new ApiSuccessResult<UpdateRolePermissionsResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(UpdateRolePermissionsAsync),
            Content = response,
        });
    }

    [RequireAdminSettingsManage]
    [HttpGet("GetGlobalSettings")]
    public async Task<ActionResult<ApiSuccessResult<GetGlobalSettingsResult>>> GetGlobalSettingsAsync(
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(new GetGlobalSettingsRequest(), cancellationToken);
        return Ok(new ApiSuccessResult<GetGlobalSettingsResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(GetGlobalSettingsAsync),
            Content = response,
        });
    }

    [RequireAdminSettingsManage]
    [HttpPost("UpdateGlobalSettings")]
    public async Task<ActionResult<ApiSuccessResult<UpdateGlobalSettingsResult>>> UpdateGlobalSettingsAsync(
        UpdateGlobalSettingsRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);
        return Ok(new ApiSuccessResult<UpdateGlobalSettingsResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(UpdateGlobalSettingsAsync),
            Content = response,
        });
    }

    [RequireAdminAuditView]
    [HttpGet("ResolveAuditDisplayNames")]
    public async Task<ActionResult<ApiSuccessResult<ResolveAuditDisplayNamesResult>>> ResolveAuditDisplayNamesAsync(
        [FromQuery] string? profileId,
        [FromQuery] string? technicalTicketId,
        [FromQuery] string? customerId,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(
            new ResolveAuditDisplayNamesRequest
            {
                ProfileId = profileId,
                TechnicalTicketId = technicalTicketId,
                CustomerId = customerId,
            },
            cancellationToken);
        return Ok(new ApiSuccessResult<ResolveAuditDisplayNamesResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(ResolveAuditDisplayNamesAsync),
            Content = response,
        });
    }

    [RequireAdminAuditView]
    [HttpGet("GetUserAuditLogList")]
    public async Task<ActionResult<ApiSuccessResult<GetUserAuditLogListResult>>> GetUserAuditLogListAsync(
        [FromQuery] GetUserAuditLogListRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);
        return Ok(new ApiSuccessResult<GetUserAuditLogListResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(GetUserAuditLogListAsync),
            Content = response,
        });
    }

    [RequireAdminUsersManage]
    [HttpGet("GetOrgUnitList")]
    public async Task<ActionResult<ApiSuccessResult<GetOrgUnitListResult>>> GetOrgUnitListAsync(
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(new GetOrgUnitListRequest(), cancellationToken);
        return Ok(new ApiSuccessResult<GetOrgUnitListResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(GetOrgUnitListAsync),
            Content = response,
        });
    }

    [RequireAdminUsersManage]
    [HttpPost("CreateOrgUnit")]
    public async Task<ActionResult<ApiSuccessResult<CreateOrgUnitResult>>> CreateOrgUnitAsync(
        CreateOrgUnitRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);
        return Ok(new ApiSuccessResult<CreateOrgUnitResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(CreateOrgUnitAsync),
            Content = response,
        });
    }

    [RequireAdminUsersManage]
    [HttpPost("UpdateOrgUnit")]
    public async Task<ActionResult<ApiSuccessResult<UpdateOrgUnitResult>>> UpdateOrgUnitAsync(
        UpdateOrgUnitRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);
        return Ok(new ApiSuccessResult<UpdateOrgUnitResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(UpdateOrgUnitAsync),
            Content = response,
        });
    }

    [RequireAuthenticatedOperator]
    [HttpPost("GetPersonaMenuNavigation")]
    public async Task<ActionResult<ApiSuccessResult<GetPersonaMenuNavigationResult>>> GetPersonaMenuNavigationAsync(
        GetPersonaMenuNavigationRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);
        return Ok(new ApiSuccessResult<GetPersonaMenuNavigationResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(GetPersonaMenuNavigationAsync),
            Content = response,
        });
    }

}
