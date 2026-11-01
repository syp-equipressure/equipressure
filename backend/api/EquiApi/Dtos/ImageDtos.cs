using EquiApi.Core.Services;
using EquiApi.Util;
using FluentValidation;

namespace EquiApi.Dtos;

/// <summary>
///     Request to set the image of a horse or a person
/// </summary>
/// <param name="File">The image, at most 5 MB</param>
public sealed record UploadImageRequest(IFormFile? File)
{
    /// <summary>
    ///     The maximum size of an image in bytes
    /// </summary>
    public const int MaxFileSize = 5 * 1024 * 1024;

    // no SVG, it may contain scripts
    private static readonly HashSet<string> allowedContentTypes =
        ["image/jpeg", "image/png", "image/webp", "image/gif"];

    public async ValueTask<ImageData> ToDataAsync(CancellationToken cancellationToken)
    {
        using var content = new MemoryStream();
        await File!.CopyToAsync(content, cancellationToken);

        return new ImageData(File.ResolvedContentType, content.ToArray());
    }

    public sealed class Validator : AbstractValidator<UploadImageRequest>
    {
        public Validator()
        {
            RuleFor(x => x.File).NotNull()
                                .Must(file => file is { Length: > 0 and <= MaxFileSize })
                                .WithMessage("The image has to be between 1 byte and 5 MB")
                                .Must(file => file is not null
                                              && allowedContentTypes.Contains(file.ResolvedContentType))
                                .WithMessage("Only JPEG, PNG, WebP and GIF images are allowed");
        }
    }
}