using Application.Features.SalesQuotationManager.Commands;
using Application.Features.SalesQuotationManager.Queries;
using ASPNET.BackEnd.Common.Base;
using ASPNET.BackEnd.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ASPNET.BackEnd.Controllers;

[Route("api/[controller]")]
public class SalesQuotationController : BaseApiController
{
    public SalesQuotationController(ISender sender) : base(sender)
    {
    }

    [Authorize]
    [HttpPost("CreateSalesQuotation")]
    public async Task<ActionResult<ApiSuccessResult<CreateSalesQuotationResult>>> CreateSalesQuotationAsync(CreateSalesQuotationRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<CreateSalesQuotationResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(CreateSalesQuotationAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpPost("UpdateSalesQuotation")]
    public async Task<ActionResult<ApiSuccessResult<UpdateSalesQuotationResult>>> UpdateSalesQuotationAsync(UpdateSalesQuotationRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<UpdateSalesQuotationResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(UpdateSalesQuotationAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpPost("DeleteSalesQuotation")]
    public async Task<ActionResult<ApiSuccessResult<DeleteSalesQuotationResult>>> DeleteSalesQuotationAsync(DeleteSalesQuotationRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<DeleteSalesQuotationResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(DeleteSalesQuotationAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpGet("GetSalesQuotationList")]
    public async Task<ActionResult<ApiSuccessResult<GetSalesQuotationListResult>>> GetSalesQuotationListAsync(
        CancellationToken cancellationToken,
        [FromQuery] bool isDeleted = false
        )
    {
        var request = new GetSalesQuotationListRequest { IsDeleted = isDeleted };
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<GetSalesQuotationListResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetSalesQuotationListAsync)}",
            Content = response
        });
    }


    [Authorize]
    [HttpGet("GetSalesQuotationStatusList")]
    public async Task<ActionResult<ApiSuccessResult<GetSalesQuotationStatusListResult>>> GetSalesQuotationStatusListAsync(
        CancellationToken cancellationToken
        )
    {
        var request = new GetSalesQuotationStatusListRequest { };
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<GetSalesQuotationStatusListResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetSalesQuotationStatusListAsync)}",
            Content = response
        });
    }


    [Authorize]
    [HttpGet("GetSalesQuotationSingle")]
    public async Task<ActionResult<ApiSuccessResult<GetSalesQuotationSingleResult>>> GetSalesQuotationSingleAsync(
    CancellationToken cancellationToken,
    [FromQuery] string id
    )
    {
        var request = new GetSalesQuotationSingleRequest { Id = id };
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<GetSalesQuotationSingleResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetSalesQuotationSingleAsync)}",
            Content = response
        });
    }


}


