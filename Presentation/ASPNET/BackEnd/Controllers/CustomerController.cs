using Application.Features.CustomerManager.Commands;
using Application.Features.CustomerManager.Queries;
using ASPNET.BackEnd.Common.Base;
using ASPNET.BackEnd.Common.Models;
using Infrastructure.SecurityManager.Roles;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ASPNET.BackEnd.Controllers;

[Route("api/[controller]")]
public class CustomerController : BaseApiController
{
    public CustomerController(ISender sender) : base(sender)
    {
    }

    [Authorize]
    [HttpPost("CreateCustomer")]
    public async Task<ActionResult<ApiSuccessResult<CreateCustomerResult>>> CreateCustomerAsync(CreateCustomerRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<CreateCustomerResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(CreateCustomerAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpPost("UpdateCustomer")]
    public async Task<ActionResult<ApiSuccessResult<UpdateCustomerResult>>> UpdateCustomerAsync(UpdateCustomerRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<UpdateCustomerResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(UpdateCustomerAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpPost("DeleteCustomer")]
    public async Task<ActionResult<ApiSuccessResult<DeleteCustomerResult>>> DeleteCustomerAsync(DeleteCustomerRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<DeleteCustomerResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(DeleteCustomerAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpGet("GetCustomerList")]
    public async Task<ActionResult<ApiSuccessResult<GetCustomerListResult>>> GetCustomerListAsync(
        CancellationToken cancellationToken,
        [FromQuery] bool isDeleted = false,
        [FromQuery] string? customerId = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 0
        )
    {
        var request = new GetCustomerListRequest { IsDeleted = isDeleted, CustomerId = customerId, Skip = skip, Take = take };
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<GetCustomerListResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetCustomerListAsync)}",
            Content = response
        });
    }

    /// <summary>Pre-check before creating a subscriber: find existing CRM parties by national ID or phone.</summary>
    [Authorize]
    [HttpGet("FindCustomerCandidates")]
    public async Task<ActionResult<ApiSuccessResult<FindCustomerCandidatesResult>>> FindCustomerCandidatesAsync(
        CancellationToken cancellationToken,
        [FromQuery] string? nationalId = null,
        [FromQuery] string? phone = null)
    {
        var request = new FindCustomerCandidatesRequest { NationalId = nationalId, Phone = phone };
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<FindCustomerCandidatesResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(FindCustomerCandidatesAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpGet("GetCustomer360")]
    public async Task<ActionResult<ApiSuccessResult<GetCustomer360Result>>> GetCustomer360Async(
        CancellationToken cancellationToken,
        [FromQuery] string customerId)
    {
        var response = await _sender.Send(new GetCustomer360Request { CustomerId = customerId ?? "" }, cancellationToken);
        return Ok(new ApiSuccessResult<GetCustomer360Result>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(GetCustomer360Async),
            Content = response
        });
    }

    [Authorize]
    [HttpGet("GetCustomer360Profile")]
    public async Task<ActionResult<ApiSuccessResult<GetCustomer360ProfileResult>>> GetCustomer360ProfileAsync(
        CancellationToken cancellationToken,
        [FromQuery] string customerId)
    {
        var response = await _sender.Send(new GetCustomer360ProfileRequest { CustomerId = customerId ?? "" }, cancellationToken);
        return Ok(new ApiSuccessResult<GetCustomer360ProfileResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(GetCustomer360ProfileAsync),
            Content = response
        });
    }

    [Authorize]
    [HttpGet("GetCustomer360Supplements")]
    public async Task<ActionResult<ApiSuccessResult<GetCustomer360SupplementsResult>>> GetCustomer360SupplementsAsync(
        CancellationToken cancellationToken,
        [FromQuery] string customerId)
    {
        var response = await _sender.Send(new GetCustomer360SupplementsRequest { CustomerId = customerId ?? "" }, cancellationToken);
        return Ok(new ApiSuccessResult<GetCustomer360SupplementsResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(GetCustomer360SupplementsAsync),
            Content = response
        });
    }

    [Authorize]
    [HttpGet("GetCustomer360LineWallets")]
    public async Task<ActionResult<ApiSuccessResult<GetCustomer360LineWalletsResult>>> GetCustomer360LineWalletsAsync(
        CancellationToken cancellationToken,
        [FromQuery] string customerId)
    {
        var response = await _sender.Send(new GetCustomer360LineWalletsRequest { CustomerId = customerId ?? "" }, cancellationToken);
        return Ok(new ApiSuccessResult<GetCustomer360LineWalletsResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(GetCustomer360LineWalletsAsync),
            Content = response
        });
    }

    [Authorize(Roles = TelecomRoles.RolesCreateOperation)]
    [HttpPost("RechargeCustomer360Line")]
    public async Task<ActionResult<ApiSuccessResult<RechargeCustomer360LineResult>>> RechargeCustomer360LineAsync(
        RechargeCustomer360LineRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);
        return Ok(new ApiSuccessResult<RechargeCustomer360LineResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(RechargeCustomer360LineAsync),
            Content = response
        });
    }

    [Authorize(Roles = TelecomRoles.RolesViewDecryptedPII)]
    [HttpGet("RevealNationalId")]
    public async Task<ActionResult<ApiSuccessResult<RevealCustomerNationalIdResult>>> RevealNationalIdAsync(
        [FromQuery] string customerId,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(new RevealCustomerNationalIdRequest { CustomerId = customerId ?? "" }, cancellationToken);
        return Ok(new ApiSuccessResult<RevealCustomerNationalIdResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(RevealNationalIdAsync),
            Content = response
        });
    }
}


