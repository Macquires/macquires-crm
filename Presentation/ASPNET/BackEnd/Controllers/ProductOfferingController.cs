using Application.Features.ProductCatalogManager.Commands;
using Application.Features.ProductCatalogManager.Queries;
using ASPNET.BackEnd.Common.Base;
using ASPNET.BackEnd.Common.Models;
using ASPNET.BackEnd.Common.Attributes;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ASPNET.BackEnd.Controllers;

[Route("api/[controller]")]
public class ProductOfferingController : BaseApiController
{
    public ProductOfferingController(ISender sender) : base(sender)
    {
    }

    [RequireProductCatalogManage]
    [HttpPost("CreateProductOffering")]
    public async Task<ActionResult<ApiSuccessResult<CreateProductOfferingResult>>> CreateProductOfferingAsync(
        CreateProductOfferingRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<CreateProductOfferingResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(CreateProductOfferingAsync)}",
            Content = response
        });
    }

    [RequireProductCatalogManage]
    [HttpPost("UpdateProductOffering")]
    public async Task<ActionResult<ApiSuccessResult<UpdateProductOfferingResult>>> UpdateProductOfferingAsync(
        UpdateProductOfferingRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<UpdateProductOfferingResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(UpdateProductOfferingAsync)}",
            Content = response
        });
    }

    [RequireProductCatalogManage]
    [HttpPost("DeleteProductOffering")]
    public async Task<ActionResult<ApiSuccessResult<DeleteProductOfferingResult>>> DeleteProductOfferingAsync(
        DeleteProductOfferingRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<DeleteProductOfferingResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(DeleteProductOfferingAsync)}",
            Content = response
        });
    }

    [RequireProductCatalogRead]
    [HttpGet("GetProductOfferingList")]
    public async Task<ActionResult<ApiSuccessResult<GetProductOfferingListResult>>> GetProductOfferingListAsync(
        CancellationToken cancellationToken,
        [FromQuery] bool isDeleted = false)
    {
        var request = new GetProductOfferingListRequest { IsDeleted = isDeleted };
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<GetProductOfferingListResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetProductOfferingListAsync)}",
            Content = response
        });
    }

    [RequireProductCatalogRead]
    [HttpGet("GetProductOfferingSingle")]
    public async Task<ActionResult<ApiSuccessResult<GetProductOfferingSingleResult>>> GetProductOfferingSingleAsync(
        CancellationToken cancellationToken,
        [FromQuery] string id)
    {
        var request = new GetProductOfferingSingleRequest { Id = id };
        var response = await _sender.Send(request, cancellationToken);

        if (response == null)
        {
            return NotFound();
        }

        return Ok(new ApiSuccessResult<GetProductOfferingSingleResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetProductOfferingSingleAsync)}",
            Content = response
        });
    }
}
