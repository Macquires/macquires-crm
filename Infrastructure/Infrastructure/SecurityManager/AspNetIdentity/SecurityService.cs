using Application.Common.Audit;
using Application.Common.Security;
using Application.Common.Services.EmailManager;
using Application.Common.Services.SecurityManager;
using Application.Common.Settings;
using Application.Common.Telecom;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.DataAccessManager.EFCore.Contexts;
using Infrastructure.SecurityManager.NavigationMenu;
using Infrastructure.SecurityManager.Roles;
using Infrastructure.SecurityManager.Tokens;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System.Data;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using static Domain.Common.Constants;

namespace Infrastructure.SecurityManager.AspNetIdentity;

public class SecurityService : ISecurityService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ITokenService _tokenService;
    private readonly DataContext _context;
    private readonly IdentitySettings _identitySettings;
    private readonly IEmailService _emailService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IConfiguration _configuration;
    private readonly INavigationMenuService _navigationMenu;
    private readonly IUserScopeService _userScope;
    private readonly IUserAuditService _audit;
    private readonly IGlobalSettingsProvider _globalSettings;
    private readonly IPermissionEvaluator _permissionEvaluator;

    public SecurityService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ITokenService tokenService,
        DataContext context,
        IOptions<IdentitySettings> identitySettings,
        IEmailService emailService,
        IHttpContextAccessor httpContextAccessor,
        RoleManager<IdentityRole> roleManager,
        IConfiguration configuration,
        INavigationMenuService navigationMenu,
        IUserScopeService userScope,
        IUserAuditService audit,
        IGlobalSettingsProvider globalSettings,
        IPermissionEvaluator permissionEvaluator
        )
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
        _context = context;
        _identitySettings = identitySettings.Value;
        _emailService = emailService;
        _httpContextAccessor = httpContextAccessor;
        _roleManager = roleManager;
        _configuration = configuration;
        _navigationMenu = navigationMenu;
        _userScope = userScope;
        _audit = audit;
        _globalSettings = globalSettings;
        _permissionEvaluator = permissionEvaluator;
    }

    public async Task<LoginResultDto> LoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default
        )
    {
        var user = await _userManager.FindByEmailAsync(email);

        if (user == null)
        {
            await LogLoginFailedAsync(email, "بيانات دخول غير صحيحة", cancellationToken);
            throw new Exception("Invalid login credentials.");
        }

        if (user.IsBlocked == true)
        {
            await LogLoginFailedAsync(email, "المستخدم محظور", cancellationToken, user.Id);
            throw new Exception($"User is blocked. {email}");
        }

        if (user.IsDeleted == true)
        {
            await LogLoginFailedAsync(email, "المستخدم محذوف", cancellationToken, user.Id);
            throw new Exception($"User already deleted. {email}");
        }

        var result = await _signInManager.PasswordSignInAsync(user, password, true, lockoutOnFailure: false);

        if (result.IsLockedOut)
        {
            await LogLoginFailedAsync(email, "الحساب مقفل", cancellationToken, user.Id);
            throw new Exception("Invalid login credentials. IsLockedOut.");
        }

        if (!result.Succeeded)
        {
            await LogLoginFailedAsync(email, "كلمة مرور غير صحيحة", cancellationToken, user.Id);
            throw new Exception("Invalid login credentials. NotSucceeded.");
        }

        var roles = await _userManager.GetRolesAsync(user);
        var rolesList = roles.ToList();
        var roleClaims = rolesList.ConvertAll(r => new Claim(ClaimTypes.Role, r));

        var (menuNodes, primaryPersona, landingPath, permissionList) =
            await BuildSessionNavigationAsync(user, rolesList, cancellationToken);

        var jwtClaims = new List<Claim>(roleClaims);
        if (primaryPersona.HasValue)
        {
            jwtClaims.Add(new Claim(TelecomAuthClaims.PrimaryMenuPersona, primaryPersona.Value.ToString()));
        }

        var branchId = await ResolveOperatorBranchIdAsync(user, cancellationToken);
        if (!string.IsNullOrEmpty(branchId))
        {
            jwtClaims.Add(new Claim(TelecomAuthClaims.BranchId, branchId));
        }

        var jwtMinutes = await GetJwtExpiryMinutesAsync(cancellationToken);
        var accessToken = _tokenService.GenerateToken(user, jwtClaims, jwtMinutes);
        var refreshToken = _tokenService.GenerateRefreshToken();

        var tokens = await _context.Token.Where(x => x.UserId == user.Id).ToListAsync(cancellationToken);
        foreach (var item in tokens)
        {
            _context.Remove(item);
        }

        var token = new Token();
        token.UserId = user.Id;
        token.RefreshToken = refreshToken;
        token.ExpiryDate = DateTime.UtcNow.AddDays(TokenConsts.ExpiryInDays);
        token.IsDeleted = false;
        token.CreatedAtUtc = DateTime.UtcNow;
        token.CreatedById = user.Id;
        user.LastLoginAtUtc = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        await _context.AddAsync(token, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        SetAccessTokenCookie(accessToken);

        await _audit.LogAsync(
            new UserAuditLogRequest
            {
                ActorUserId = user.Id,
                UserId = user.Id,
                ActionType = UserAuditActionTypes.UserLoggedIn,
                EntityType = nameof(ApplicationUser),
                EntityId = user.Id,
                SummaryAr = "تسجيل دخول ناجح",
            },
            cancellationToken);

        return new LoginResultDto
        {
            UserId = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            CompanyName = user.CompanyName,
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            MenuNavigation = menuNodes,
            Roles = rolesList,
            Avatar = user.ProfilePictureName,
            PrimaryMenuPersona = primaryPersona?.ToString(),
            LandingPath = landingPath,
            Permissions = permissionList,
        };
    }

    public async Task<LogoutResultDto> LogoutAsync(
        string userId,
        CancellationToken cancellationToken = default
        )
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user != null)
        {
            var tokens = await _context.Token.Where(x => x.UserId == user.Id).ToListAsync(cancellationToken);
            foreach (var item in tokens)
            {
                _context.Remove(item);
            }
            await _context.SaveChangesAsync(cancellationToken);

            await _audit.LogAsync(
                new UserAuditLogRequest
                {
                    ActorUserId = user.Id,
                    UserId = user.Id,
                    ActionType = UserAuditActionTypes.UserLoggedOut,
                    EntityType = nameof(ApplicationUser),
                    EntityId = user.Id,
                    SummaryAr = "تسجيل خروج",
                },
                cancellationToken);
        }

        return new LogoutResultDto
        {
            UserId = user?.Id,
            Email = user?.Email,
            FirstName = user?.FirstName,
            LastName = user?.LastName,
            CompanyName = user?.CompanyName,
            UserClaims = null,
            AccessToken = null,
            RefreshToken = null,

        };
    }
    public async Task<RegisterResultDto> RegisterAsync(
        string email,
        string password,
        string confirmPassword,
        string firstName,
        string lastName,
        string companyName = "",
        CancellationToken cancellationToken = default
        )
    {
        if (!password.Equals(confirmPassword))
        {
            throw new Exception($"Password and ConfirmPassword is different.");
        }

        var user = new ApplicationUser(
            email,
            firstName,
            lastName,
            companyName
        );

        user.EmailConfirmed = !_identitySettings.SignIn.RequireConfirmedEmail;
        var result = await _userManager.CreateAsync(user, password);

        if (!result.Succeeded)
        {
            throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        if (!await _userManager.IsInRoleAsync(user, RoleHelper.GetProfileRole()))
        {
            await _userManager.AddToRoleAsync(user, RoleHelper.GetProfileRole());
        }

        var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

        if (_identitySettings.SignIn.RequireConfirmedEmail)
        {
            var request = _httpContextAccessor?.HttpContext?.Request;
            var callbackUrl = $"{request?.Scheme}://{request?.Host}/Accounts/EmailConfirm?email={user.Email}&code={code}";
            var encodeCallbackUrl = $"{HtmlEncoder.Default.Encode(callbackUrl)}";

            var emailSubject = $"Confirm your email";
            var emailMessage = $"Please confirm your account by <a href='{encodeCallbackUrl}'>clicking here</a>.";

            await _emailService.SendEmailAsync(user.Email ?? "", emailSubject, emailMessage);

        }

        return new RegisterResultDto
        {
            UserId = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            CompanyName = user.CompanyName
        };
    }

    public async Task<string> ConfirmEmailAsync(
        string email,
        string code,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(email);

        if (user == null)
        {
            throw new Exception($"Unable to load user with email: {email}");
        }

        code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
        var result = await _userManager.ConfirmEmailAsync(user, code);

        if (!result.Succeeded)
        {
            throw new Exception($"Error confirming your email: {email}");
        }

        return email;
    }

    public async Task<string> ForgotPasswordAsync(
        string email,
        CancellationToken cancellationToken = default
        )
    {
        var user = await _userManager.FindByEmailAsync(email);

        if (user == null)
        {
            throw new Exception($"Unable to load user with email: {email}");
        }

        var code = await _userManager.GeneratePasswordResetTokenAsync(user);
        code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

        var textTempPassword = Guid.NewGuid().ToString().Substring(0, _identitySettings.Password.RequiredLength);
        var encryptedTempPassword = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(textTempPassword));

        var request = _httpContextAccessor?.HttpContext?.Request;
        var callbackUrl = $"{request?.Scheme}://{request?.Host}/Accounts/ForgotPasswordConfirmation?email={user.Email}&code={code}&tempPassword={encryptedTempPassword}";
        var encodeCallbackUrl = $"{HtmlEncoder.Default.Encode(callbackUrl)}";

        var emailSubject = $"Forgot password confirmation";
        var emailMessage = $"Your temporary password is: <strong>{textTempPassword}</strong>. Please confirm reset your password by <a href='{encodeCallbackUrl}'>clicking here</a>.";

        await _emailService.SendEmailAsync(user.Email ?? "", emailSubject, emailMessage);

        return "A temporary password has been sent to the registered email address.";

    }

    public async Task<string> ForgotPasswordConfirmationAsync(
        string email,
        string tempPassword,
        string code,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(email);

        if (user == null)
        {
            throw new Exception($"Unable to load user with email: {email}");
        }

        code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
        tempPassword = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(tempPassword));
        var result = await _userManager.ResetPasswordAsync(user, code, tempPassword);

        if (!result.Succeeded)
        {
            throw new Exception($"Error resetting your password");
        }

        return email;
    }

    public async Task<RefreshTokenResultDto> RefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken
        )
    {
        var registeredToken = await _context.Token.SingleOrDefaultAsync(x => x.RefreshToken == refreshToken, cancellationToken);
        if (registeredToken == null)
        {
            throw new Exception("Refresh token invalid, please re-login");
        }
        var user = await _userManager.FindByIdAsync(registeredToken?.UserId ?? "");
        if (user == null)
        {
            throw new Exception("Refresh token invalid, please re-login");
        }
        _context.Token.Remove(registeredToken!);

        var roles = await _userManager.GetRolesAsync(user);
        var rolesList = roles.ToList();
        var roleClaims = rolesList.ConvertAll(r => new Claim(ClaimTypes.Role, r));

        var (menuNodes, primaryPersona, landingPath, permissionList) =
            await BuildSessionNavigationAsync(user, rolesList, cancellationToken);

        var jwtClaims = new List<Claim>(roleClaims);
        if (primaryPersona.HasValue)
        {
            jwtClaims.Add(new Claim(TelecomAuthClaims.PrimaryMenuPersona, primaryPersona.Value.ToString()));
        }

        var branchId = await ResolveOperatorBranchIdAsync(user, cancellationToken);
        if (!string.IsNullOrEmpty(branchId))
        {
            jwtClaims.Add(new Claim(TelecomAuthClaims.BranchId, branchId));
        }

        var jwtMinutes = await GetJwtExpiryMinutesAsync(cancellationToken);
        var newAccessToken = _tokenService.GenerateToken(user, jwtClaims, jwtMinutes);
        var newRefreshToken = _tokenService.GenerateRefreshToken();

        var token = new Token();
        token.UserId = user.Id;
        token.RefreshToken = newRefreshToken;
        token.ExpiryDate = DateTime.UtcNow.AddDays(TokenConsts.ExpiryInDays);
        token.IsDeleted = false;
        token.CreatedAtUtc = DateTime.UtcNow;
        token.CreatedById = user.Id;
        await _context.AddAsync(token, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        SetAccessTokenCookie(newAccessToken);

        return new RefreshTokenResultDto
        {
            UserId = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            CompanyName = user.CompanyName,
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
            MenuNavigation = menuNodes,
            Roles = rolesList,
            Avatar = user.ProfilePictureName,
            PrimaryMenuPersona = primaryPersona?.ToString(),
            LandingPath = landingPath,
            Permissions = permissionList,
        };
    }

    private async Task<(
        List<MenuNavigationTreeNodeDto> Menu,
        TelecomMenuPersona? Persona,
        string LandingPath,
        List<string> Permissions)> BuildSessionNavigationAsync(
        ApplicationUser user,
        List<string> rolesList,
        CancellationToken cancellationToken)
    {
        var permissionKeys = await _permissionEvaluator.GetUserPermissionKeysAsync(user.Id, cancellationToken);
        var permissionSet = permissionKeys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var menuNodes = _navigationMenu.GetMenuForRoles(rolesList, permissionKeys: permissionSet);
        var primaryPersona = TelecomPersonaResolver.ResolvePrimary(rolesList, user.PrimaryMenuPersona);
        var landingPath = TelecomWorkspaceRules.ResolveSessionLandingPath(
            rolesList,
            primaryPersona,
            permissionSet);
        return (menuNodes, primaryPersona, landingPath, permissionKeys.ToList());
    }

    public async Task<List<GetMyProfileListResultDto>> GetMyProfileListAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var profiles = await _context.Users
            .Where(x => x.Id == userId)
            .Select(x => new GetMyProfileListResultDto
            {
                Id = x.Id,
                FirstName = x.FirstName,
                LastName = x.LastName,
                CompanyName = x.CompanyName
            })
            .ToListAsync(cancellationToken);

        return profiles;
    }

    public async Task UpdateMyProfileAsync(
        string userId,
        string firstName,
        string lastName,
        string companyName,
        CancellationToken cancellationToken
        )
    {
        var user = await _context.Users.Where(x => x.Id == userId).SingleOrDefaultAsync(cancellationToken);

        if (user == null)
        {
            throw new Exception($"Unable to load user with id: {userId}");
        }

        user.FirstName = firstName;
        user.LastName = lastName;
        user.CompanyName = companyName;

        _context.Update(user);
        await _context.SaveChangesAsync(cancellationToken);
    }
    public async Task ChangePasswordAsync(
        string userId,
        string oldPassword,
        string newPassword,
        string confirmNewPassword,
        CancellationToken cancellationToken
    )
    {
        if (newPassword != confirmNewPassword)
        {
            throw new Exception("New password and confirm password do not match.");
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            throw new Exception($"Unable to load user with id: {userId}");
        }

        var result = await _userManager.ChangePasswordAsync(user, oldPassword, newPassword);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new Exception($"Password change failed: {errors}");
        }

        var isDemoVersion = _configuration.GetValue<bool>("IsDemoVersion");
        if (isDemoVersion && user.Email == _identitySettings.DefaultAdmin.Email)
        {
            throw new Exception($"Update default admin password is not allowed in demo version.");
        }
    }

    public async Task<CloneRolePermissionsResultDto> CloneRolePermissionsAsync(
        string sourceRoleName,
        string newRoleName,
        string? createdById = null,
        CancellationToken cancellationToken = default)
    {
        var source = sourceRoleName.Trim();
        var target = newRoleName.Trim();

        if (await _roleManager.RoleExistsAsync(target))
        {
            throw new InvalidOperationException($"الدور '{target}' موجود مسبقاً.");
        }

        if (!await _roleManager.RoleExistsAsync(source))
        {
            throw new InvalidOperationException($"الدور المصدر '{source}' غير موجود.");
        }

        await _roleManager.CreateAsync(new IdentityRole(target));

        var keys = await _context.RolePermission
            .AsNoTracking()
            .Where(rp => rp.RoleName == source)
            .Select(rp => rp.PermissionKey)
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        foreach (var key in keys.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            await _context.RolePermission.AddAsync(new RolePermission
            {
                RoleName = target,
                PermissionKey = key,
                GrantedAtUtc = now,
                GrantedById = createdById,
            }, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return new CloneRolePermissionsResultDto
        {
            NewRoleName = target,
            PermissionKeys = keys,
        };
    }

    public async Task<List<GetRoleListResultDto>> GetRoleListAsync(
        CancellationToken cancellationToken = default)
    {
        return await _roleManager.Roles
            .Select(x => new GetRoleListResultDto
            {
                Id = x.Id,
                Name = x.Name ?? string.Empty,
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<List<GetUserListResultDto>> GetUserListAsync(
        string? actorUserId,
        CancellationToken cancellationToken = default
        )
    {
        HashSet<string>? visibleIds = null;
        if (!string.IsNullOrEmpty(actorUserId))
        {
            visibleIds = await _userScope.GetVisibleUserIdsAsync(actorUserId, cancellationToken);
        }

        var query = _userManager.Users.Where(x => x.IsDeleted != true);
        if (visibleIds != null)
        {
            query = query.Where(x => visibleIds.Contains(x.Id));
        }

        var users = await query
            .OrderBy(x => x.FirstName)
            .ThenBy(x => x.LastName)
            .ToListAsync(cancellationToken);

        if (users.Count == 0)
        {
            return [];
        }

        var userIds = users.Select(u => u.Id).ToList();
        var managerIds = users
            .Where(u => !string.IsNullOrEmpty(u.ManagerUserId))
            .Select(u => u.ManagerUserId!)
            .Distinct()
            .ToList();

        var managers = await _context.Users
            .AsNoTracking()
            .Where(u => managerIds.Contains(u.Id))
            .Select(u => new { u.Id, u.FirstName, u.LastName })
            .ToDictionaryAsync(u => u.Id, cancellationToken);

        var orgUnitIds = users
            .Where(u => !string.IsNullOrEmpty(u.OrgUnitId))
            .Select(u => u.OrgUnitId!)
            .Distinct()
            .ToList();

        var orgUnits = await _context.OrgUnit
            .AsNoTracking()
            .Where(o => orgUnitIds.Contains(o.Id))
            .Select(o => new { o.Id, o.NameAr })
            .ToDictionaryAsync(o => o.Id, cancellationToken);

        var roleRows = await (
            from ur in _context.UserRoles
            join r in _context.Roles on ur.RoleId equals r.Id
            where userIds.Contains(ur.UserId)
            select new { ur.UserId, RoleName = r.Name }
        ).ToListAsync(cancellationToken);

        var rolesByUser = roleRows
            .GroupBy(x => x.UserId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.RoleName ?? "").Where(n => n.Length > 0).ToList());

        var onlineThreshold = DateTime.UtcNow.AddMinutes(-30);

        return users.Select(u =>
        {
            var roles = rolesByUser.TryGetValue(u.Id, out var r) ? r : [];
            var lastSeen = u.LastActivityAtUtc ?? u.LastLoginAtUtc;
            managers.TryGetValue(u.ManagerUserId ?? "", out var mgr);
            orgUnits.TryGetValue(u.OrgUnitId ?? "", out var ou);

            return new GetUserListResultDto
            {
                Id = u.Id,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Email = u.Email,
                IsBlocked = u.IsBlocked,
                IsDeleted = u.IsDeleted,
                EmailConfirmed = u.EmailConfirmed,
                CreatedAt = u.CreatedAt,
                PrimaryMenuPersona = u.PrimaryMenuPersona?.ToString()
                    ?? TelecomPersonaResolver.ResolveFromRoles(roles)?.ToString(),
                ManagerUserId = u.ManagerUserId,
                ManagerDisplayName = mgr == null
                    ? null
                    : $"{mgr.FirstName} {mgr.LastName}".Trim(),
                OrgUnitId = u.OrgUnitId,
                OrgUnitNameAr = ou?.NameAr,
                LastLoginAtUtc = u.LastLoginAtUtc,
                LastActivityAtUtc = u.LastActivityAtUtc,
                Roles = roles,
                RolesDisplay = string.Join(", ", roles.OrderBy(x => x)),
                IsOnline = u.IsBlocked != true && lastSeen.HasValue && lastSeen.Value >= onlineThreshold,
            };
        }).ToList();
    }

    private async Task SyncTelecomRoleFromPersonaAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        if (!user.PrimaryMenuPersona.HasValue)
        {
            return;
        }

        var targetRole = TelecomPersonaRoleMapper.GetRoleForPersona(user.PrimaryMenuPersona.Value);
        if (string.IsNullOrEmpty(targetRole))
        {
            return;
        }

        var current = await _userManager.GetRolesAsync(user);
        foreach (var role in TelecomPersonaRoleMapper.TelecomRolesToReplace)
        {
            if (current.Contains(role) && !string.Equals(role, targetRole, StringComparison.OrdinalIgnoreCase))
            {
                await _userManager.RemoveFromRoleAsync(user, role);
            }
        }

        if (!await _userManager.IsInRoleAsync(user, targetRole))
        {
            await _userManager.AddToRoleAsync(user, targetRole);
        }
    }

    private async Task<int> GetJwtExpiryMinutesAsync(CancellationToken cancellationToken)
    {
        var fallback = _configuration.GetValue("Jwt:ExpireInMinute", 60);
        return await _globalSettings.GetIntAsync(
            GlobalSettingKeys.JwtAccessTokenMinutes,
            fallback,
            min: 5,
            max: 1440,
            cancellationToken);
    }

    private Task LogLoginFailedAsync(
        string email,
        string reasonAr,
        CancellationToken cancellationToken,
        string? userId = null) =>
        _audit.LogAsync(
            new UserAuditLogRequest
            {
                ActorUserId = userId ?? "anonymous",
                UserId = userId,
                ActionType = UserAuditActionTypes.UserLoginFailed,
                EntityType = nameof(ApplicationUser),
                EntityId = userId,
                SummaryAr = $"فشل تسجيل الدخول: {reasonAr}",
                Payload = new { email, reasonAr },
            },
            cancellationToken);

    private void SetAccessTokenCookie(string accessToken)
    {
        var response = _httpContextAccessor.HttpContext?.Response;
        if (response == null || string.IsNullOrEmpty(accessToken))
        {
            return;
        }

        var minutes = _configuration.GetSection("Jwt").GetValue<int>("ExpireInMinute");
        if (minutes <= 0)
        {
            minutes = 480;
        }

        response.Cookies.Append(
            "accessToken",
            accessToken,
            new CookieOptions
            {
                Path = "/",
                HttpOnly = true,
                Secure = _httpContextAccessor.HttpContext?.Request.IsHttps == true,
                SameSite = SameSiteMode.Lax,
                MaxAge = TimeSpan.FromMinutes(minutes),
                IsEssential = true,
            });
    }

    public async Task<CreateUserResultDto> CreateUserAsync(
        string email,
        string password,
        string confirmPassword,
        string firstName,
        string lastName,
        bool emailConfirmed = true,
        bool isBlocked = false,
        bool isDeleted = false,
        string createdById = "",
        TelecomMenuPersona? primaryMenuPersona = null,
        string? managerUserId = null,
        string? orgUnitId = null,
        bool syncTelecomRoleFromPersona = true,
        CancellationToken cancellationToken = default
        )
    {
        if (!password.Equals(confirmPassword))
        {
            throw new Exception($"Password and ConfirmPassword is different.");
        }

        var user = new ApplicationUser(
            email,
            firstName,
            lastName
        );

        user.EmailConfirmed = emailConfirmed;
        user.IsBlocked = isBlocked;
        user.IsDeleted = isDeleted;
        user.CreatedById = createdById;
        user.PrimaryMenuPersona = primaryMenuPersona;
        user.ManagerUserId = string.IsNullOrWhiteSpace(managerUserId) ? null : managerUserId.Trim();
        user.OrgUnitId = string.IsNullOrWhiteSpace(orgUnitId) ? null : orgUnitId.Trim();

        var result = await _userManager.CreateAsync(user, password);

        if (!result.Succeeded)
        {
            throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        if (!await _userManager.IsInRoleAsync(user, RoleHelper.GetProfileRole()))
        {
            await _userManager.AddToRoleAsync(user, RoleHelper.GetProfileRole());
        }

        if (syncTelecomRoleFromPersona && primaryMenuPersona.HasValue)
        {
            await SyncTelecomRoleFromPersonaAsync(user, cancellationToken);
        }

        return new CreateUserResultDto
        {
            UserId = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            EmailConfirmed = user.EmailConfirmed,
            IsBlocked = user.IsBlocked,
            IsDeleted = user.IsDeleted,
        };
    }

    public async Task<UpdateUserResultDto> UpdateUserAsync(
        string userId,
        string firstName,
        string lastName,
        bool emailConfirmed = true,
        bool isBlocked = false,
        bool isDeleted = false,
        string updatedById = "",
        TelecomMenuPersona? primaryMenuPersona = null,
        string? managerUserId = null,
        string? orgUnitId = null,
        bool syncTelecomRoleFromPersona = true,
        CancellationToken cancellationToken = default
        )
    {
        var user = await _context.Users.Where(x => x.Id == userId).SingleOrDefaultAsync(cancellationToken);

        if (user == null)
        {
            throw new Exception($"Unable to load user with id: {userId}");
        }

        if (user.Email == _identitySettings.DefaultAdmin.Email)
        {
            throw new Exception($"Update default admin is not allowed.");
        }

        user.FirstName = firstName;
        user.LastName = lastName;
        user.EmailConfirmed = emailConfirmed;
        user.IsBlocked = isBlocked;
        user.IsDeleted = isDeleted;
        user.UpdatedById = updatedById;
        if (primaryMenuPersona.HasValue)
        {
            user.PrimaryMenuPersona = primaryMenuPersona;
        }

        user.ManagerUserId = string.IsNullOrWhiteSpace(managerUserId) ? null : managerUserId.Trim();
        user.OrgUnitId = string.IsNullOrWhiteSpace(orgUnitId) ? null : orgUnitId.Trim();

        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        if (syncTelecomRoleFromPersona && user.PrimaryMenuPersona.HasValue)
        {
            await SyncTelecomRoleFromPersonaAsync(user, cancellationToken);
        }

        return new UpdateUserResultDto
        {
            UserId = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            EmailConfirmed = user.EmailConfirmed,
            IsBlocked = user.IsBlocked,
            IsDeleted = user.IsDeleted,
        };
    }

    public async Task<DeleteUserResultDto> DeleteUserAsync(
        string userId,
        string deletedById = "",
        CancellationToken cancellationToken = default
        )
    {
        var user = await _context.Users.Where(x => x.Id == userId).SingleOrDefaultAsync(cancellationToken);

        if (user == null)
        {
            throw new Exception($"Unable to load user with id: {userId}");
        }

        if (user.Email == _identitySettings.DefaultAdmin.Email)
        {
            throw new Exception($"Update default admin is not allowed.");
        }

        user.IsDeleted = true;
        user.UpdatedById = deletedById;

        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        return new DeleteUserResultDto
        {
            UserId = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            EmailConfirmed = user.EmailConfirmed,
            IsBlocked = user.IsBlocked,
            IsDeleted = user.IsDeleted,
        };
    }

    public async Task UpdatePasswordUserAsync(
        string userId,
        string newPassword,
        CancellationToken cancellationToken
        )
    {

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            throw new Exception($"Unable to load user with id: {userId}");
        }

        var isDemoVersion = _configuration.GetValue<bool>("IsDemoVersion");
        if (isDemoVersion && user.Email == _identitySettings.DefaultAdmin.Email)
        {
            throw new Exception($"Update default admin password is not allowed in demo version.");
        }

        var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);

        var result = await _userManager.ResetPasswordAsync(user, resetToken, newPassword);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new Exception($"Password change failed: {errors}");
        }
    }

    public async Task<List<string>> GetUserRolesAsync(
        string userId,
        CancellationToken cancellationToken = default
        )
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
        {
            throw new Exception($"Unable to load user with id: {userId}");
        }

        var roles = await _userManager.GetRolesAsync(user);
        return roles.ToList();
    }

    public async Task<List<string>> UpdateUserRoleAsync(
            string userId,
            string roleName,
            bool accessGranted,
            CancellationToken cancellationToken = default
        )
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            throw new Exception($"Unable to load user with id: {userId}");
        }

        if (user.Email == _identitySettings.DefaultAdmin.Email)
        {
            throw new Exception($"Update default admin is not allowed.");
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        if (accessGranted)
        {
            if (!currentRoles.Contains(roleName))
            {
                var result = await _userManager.AddToRoleAsync(user, roleName);
                if (!result.Succeeded)
                {
                    throw new Exception($"Failed to add role '{roleName}' to user with id: {userId}. Errors: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }
        }
        else
        {
            if (currentRoles.Contains(roleName))
            {
                var result = await _userManager.RemoveFromRoleAsync(user, roleName);
                if (!result.Succeeded)
                {
                    throw new Exception($"Failed to remove role '{roleName}' from user with id: {userId}. Errors: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }
        }

        var updatedRoles = (await _userManager.GetRolesAsync(user)).ToList();
        var persona = TelecomPersonaResolver.ResolvePrimary(updatedRoles, null);
        if (persona.HasValue && user.PrimaryMenuPersona != persona)
        {
            user.PrimaryMenuPersona = persona;
            await _userManager.UpdateAsync(user);
        }

        return updatedRoles;
    }



    public async Task ChangeAvatarAsync(
        string userId,
        string avatar,
        CancellationToken cancellationToken
        )
    {
        var user = await _context.Users.Where(x => x.Id == userId).SingleOrDefaultAsync(cancellationToken);

        if (user == null)
        {
            throw new Exception($"Unable to load user with id: {userId}");
        }

        user.ProfilePictureName = avatar;

        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }

    private async Task<string?> ResolveOperatorBranchIdAsync(
        ApplicationUser user,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(user.OrgUnitId))
        {
            return null;
        }

        var kind = await _context.OrgUnit.AsNoTracking()
            .Where(o => !o.IsDeleted && o.Id == user.OrgUnitId)
            .Select(o => (OrgUnitKind?)o.Kind)
            .FirstOrDefaultAsync(cancellationToken);

        return kind == OrgUnitKind.Branch ? user.OrgUnitId.Trim() : null;
    }

}
