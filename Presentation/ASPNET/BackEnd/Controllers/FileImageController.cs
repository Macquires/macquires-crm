using Application.Features.FileImageManager.Commands;
using Application.Features.FileImageManager.Queries;
using ASPNET.BackEnd.Common.Base;
using ASPNET.BackEnd.Common.Models;
using ASPNET.BackEnd.Common.Attributes;
using Infrastructure.FileImageManager;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ASPNET.BackEnd.Controllers;

[Route("api/[controller]")]
public class FileImageController : BaseApiController
{
    public FileImageController(ISender sender) : base(sender)
    {
    }

    [RequireFileDocumentUpload]
    [HttpPost("UploadImage")]
    public async Task<ActionResult<CreateImageResult>> UploadImageAsync(IFormFile file, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("Invalid file.");
        }

        using (var memoryStream = new MemoryStream())
        {
            await file.CopyToAsync(memoryStream, cancellationToken);
            var fileData = memoryStream.ToArray();
            var extension = Path.GetExtension(file.FileName).TrimStart('.');

            var command = new CreateImageRequest
            {
                OriginalFileName = file.FileName,
                Extension = extension,
                Data = fileData,
                Size = fileData.Length
            };

            var result = await _sender.Send(command, cancellationToken);

            if (result?.ImageName == null)
            {
                return StatusCode(500, "An error occurred while uploading the image.");
            }

            return Ok(new ApiSuccessResult<CreateImageResult>
            {
                Code = StatusCodes.Status200OK,
                Message = $"Success executing {nameof(UploadImageAsync)}",
                Content = result
            });
        }
    }

    [RequireAuthenticatedOperator]
    [HttpGet("GetImage")]
    public async Task<IActionResult> GetImageAsync(
        [FromQuery] string imageName,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(imageName)
            || string.Equals(imageName, "undefined", StringComparison.OrdinalIgnoreCase)
            || string.Equals(imageName, "null", StringComparison.OrdinalIgnoreCase)
            || Path.GetExtension(imageName) == string.Empty)
        {
            imageName = "noimage.png";
        }

        try
        {
            var request = new GetImageRequest
            {
                ImageName = imageName
            };

            var result = await _sender.Send(request, cancellationToken);

            if (result?.Data == null || result.Data.Length == 0)
            {
                return NotFound("Image not found.");
            }

            var extension = Path.GetExtension(imageName).ToLower();
            var mimeType = FileImageHelper.GetMimeType(extension);

            return File(result.Data, mimeType);
        }
        catch (FileNotFoundException)
        {
            return NotFound("Image not found.");
        }
    }
}
