using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Nancy;
using Nancy.Responses;

namespace Cassette.Nancy
{
  internal class RawFileRouteHandler : CassetteRouteHandlerBase
  {
    public RawFileRouteHandler(IBundleContainer bundleContainer, string handlerRoot, string applicationRoot)
      : base(bundleContainer, handlerRoot)
    {
      this.applicationRoot = applicationRoot;
    }

    public override Response ProcessRequest(NancyContext context)
    {
      if (!context.Request.Url.Path.StartsWith(HandlerRoot, StringComparison.InvariantCultureIgnoreCase))
      {
        return null;
      }

      var path = context.Request.Url.Path.Remove(0, HandlerRoot.Length + 1);
      var match = Regex.Match(path, @"^(?<filename>.*)_[a-z0-9]+_(?<extension>[a-z]+)$", RegexOptions.IgnoreCase);
      if (match.Success == false)
      {
        //if (Logger != null) Logger.Error("RawFileRouteHandler.ProcessRequest : Invalid file path in URL '{0}'", path);
        return null;
      }
      var extension = match.Groups["extension"].Value;

      var filePath = Path.Combine(applicationRoot, string.Concat(match.Groups["filename"].Value, ".", extension).Replace('/', '\\'));
      if (!File.Exists(filePath))
      {
        //if (Logger != null) Logger.Error("RawFileRouteHandler.ProcessRequest : Raw file does not exist '{0}'", filePath);
        return null;
      }

      var givenETag = context.Request.Headers["If-None-Match"].FirstOrDefault();

      var response = givenETag == GetETag(filePath)
        ? CreateNotModifiedResponse()
        : CreateFileResponse(filePath);

      return SetCacheHeaders(response, GetETag(filePath));
    }

    private Response CreateNotModifiedResponse()
    {
      return new Response { StatusCode = HttpStatusCode.NotModified };
    }

    private static StreamResponse CreateFileResponse(string filePath)
    {
      return new StreamResponse(() => File.OpenRead(filePath), MimeTypes.GetMimeType(filePath));
    }

    private Response SetCacheHeaders(Response response, string actualETag)
    {
      response.WithHeader("Cache-Control", "public");
      response.WithHeader("ETag", actualETag);
      response.WithHeader("Expires", DateTime.UtcNow.AddYears(1).ToString("R"));
      return response;
    }

    string GetETag(string fullPath)
    {
        using (var hash = SHA1.Create())
        {
            using (var file = File.OpenRead(fullPath))
            {
                return "\"" + Convert.ToBase64String(hash.ComputeHash(file)) + "\"";
            }
        }
    }

    private readonly string applicationRoot;
  }
}