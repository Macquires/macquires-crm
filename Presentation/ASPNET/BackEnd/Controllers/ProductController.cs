using Application.Features.ProductManager.Commands;
using Application.Features.ProductManager.Queries;
using ASPNET.BackEnd.Common.Base;
using ASPNET.BackEnd.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ASPNET.BackEnd.Controllers;

[Route("api/[controller]")]
public class ProductController : BaseApiController
{
    public ProductController(ISender sender) : base(sender)
    {
    }

    [Authorize]
    [HttpPost("CreateProduct")]
    public async Task<ActionResult<ApiSuccessResult<CreateProductResult>>> CreateProductAsync(CreateProductRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<CreateProductResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(CreateProductAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpPost("UpdateProduct")]
    public async Task<ActionResult<ApiSuccessResult<UpdateProductResult>>> UpdateProductAsync(UpdateProductRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<UpdateProductResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(UpdateProductAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpPost("DeleteProduct")]
    public async Task<ActionResult<ApiSuccessResult<DeleteProductResult>>> DeleteProductAsync(DeleteProductRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<DeleteProductResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(DeleteProductAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpGet("GetProductList")]
    public async Task<ActionResult<ApiSuccessResult<GetProductListResult>>> GetProductListAsync(
        CancellationToken cancellationToken,
        [FromQuery] bool isDeleted = false
        )
    {
        var request = new GetProductListRequest { IsDeleted = isDeleted };
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<GetProductListResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetProductListAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpGet("GetMigrationEligibleProducts")]
    public async Task<ActionResult<ApiSuccessResult<GetMigrationEligibleProductsResult>>> GetMigrationEligibleProductsAsync(
        CancellationToken cancellationToken,
        [FromQuery] string subscriberProfileId = "",
        [FromQuery] string? msisdnAssetId = null,
        [FromQuery] string? targetSubscriptionTypeId = null)
    {
        var request = new GetMigrationEligibleProductsRequest
        {
            SubscriberProfileId = subscriberProfileId,
            MsisdnAssetId = msisdnAssetId,
            TargetSubscriptionTypeId = targetSubscriptionTypeId,
        };
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<GetMigrationEligibleProductsResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetMigrationEligibleProductsAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpGet("GetChangeGsmEligibleTargets")]
    public async Task<ActionResult<ApiSuccessResult<GetChangeGsmEligibleTargetsResult>>> GetChangeGsmEligibleTargetsAsync(
        CancellationToken cancellationToken,
        [FromQuery] string subscriberProfileId = "",
        [FromQuery] string? msisdnAssetId = null)
    {
        var response = await _sender.Send(
            new GetChangeGsmEligibleTargetsRequest
            {
                SubscriberProfileId = subscriberProfileId,
                MsisdnAssetId = msisdnAssetId,
            },
            cancellationToken);

        return Ok(new ApiSuccessResult<GetChangeGsmEligibleTargetsResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(GetChangeGsmEligibleTargetsAsync),
            Content = response,
        });
    }

    [Authorize]
    [HttpGet("GetEligibleVasOfferings")]
    public async Task<ActionResult<ApiSuccessResult<GetEligibleVasOfferingsResult>>> GetEligibleVasOfferingsAsync(
        CancellationToken cancellationToken,
        [FromQuery] string subscriberProfileId = "",
        [FromQuery] string? msisdnAssetId = null)
    {
        var response = await _sender.Send(
            new GetEligibleVasOfferingsRequest
            {
                SubscriberProfileId = subscriberProfileId,
                MsisdnAssetId = msisdnAssetId,
            },
            cancellationToken);

        return Ok(new ApiSuccessResult<GetEligibleVasOfferingsResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(GetEligibleVasOfferingsAsync),
            Content = response,
        });
    }

    [Authorize]
    [HttpGet("GetChangeGsmEligibleProducts")]
    public async Task<ActionResult<ApiSuccessResult<GetChangeGsmEligibleProductsResult>>> GetChangeGsmEligibleProductsAsync(
        CancellationToken cancellationToken,
        [FromQuery] string subscriberProfileId = "",
        [FromQuery] string targetSubscriptionTypeId = "",
        [FromQuery] string? msisdnAssetId = null)
    {
        var response = await _sender.Send(
            new GetChangeGsmEligibleProductsRequest
            {
                SubscriberProfileId = subscriberProfileId,
                TargetSubscriptionTypeId = targetSubscriptionTypeId,
                MsisdnAssetId = msisdnAssetId,
            },
            cancellationToken);

        return Ok(new ApiSuccessResult<GetChangeGsmEligibleProductsResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(GetChangeGsmEligibleProductsAsync),
            Content = response,
        });
    }
}


