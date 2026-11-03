using Microsoft.AspNetCore.StaticFiles;

namespace EquiApi.Util;

public static class FormFileExtensions
{
    private const string UnknownContentType = "application/octet-stream";
    private static readonly FileExtensionContentTypeProvider contentTypes = new();

    extension(IFormFile file)
    {
        /// <summary>
        ///     The media type of the file. Some clients (e.g. the app) do not send one, then it is derived from the
        ///     file extension
        /// </summary>
        public string ResolvedContentType
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(file.ContentType) && file.ContentType != UnknownContentType)
                {
                    return file.ContentType;
                }

                return contentTypes.TryGetContentType(file.FileName, out string? contentType)
                    ? contentType
                    : UnknownContentType;
            }
        }
    }
}