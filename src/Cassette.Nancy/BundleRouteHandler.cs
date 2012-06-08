using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Nancy;
using Nancy.Responses;

namespace Cassette.Nancy
{
  internal class BundleRouteHandler<T> : CassetteRouteHandlerBase
    where T : Bundle
  {
    public BundleRouteHandler(IBundleContainer bundleContainer, string handlerRoot)
      : base(bundleContainer, handlerRoot)
    {
    }

    public override Response ProcessRequest(NancyContext context)
    {
      if (!context.Request.Url.Path.StartsWith(HandlerRoot, StringComparison.InvariantCultureIgnoreCase))
      {
        return null;
      }

      var path = Regex.Replace(string.Concat("~", context.Request.Url.Path.Remove(0, HandlerRoot.Length)), @"_[^_]+$", "");

      var bundles = BundleContainer.FindBundlesContainingPath(path).ToList();
      if (bundles == null || bundles.Count != 1)
      {
        //if (Logger != null) Logger.Error("BundleRouteHandler.ProcessRequest : Bundle not found for path '{0}'", context.Request.Url.Path);
        return null;
      }
      var bundle = bundles[0];

      var actualETag = GetBundleETag(bundle);
      var givenETag = context.Request.Headers["If-None-Match"].FirstOrDefault();
      var response = givenETag == actualETag
        ? CreateNotModifiedResponse()
        : CreateBundleResponse(bundle);

      return SetCacheHeaders(response, actualETag);
    }

    private static Response CreateNotModifiedResponse()
    {
      return new Response() { StatusCode = HttpStatusCode.NotModified };
    }

    private StreamResponse CreateBundleResponse(Bundle bundle)
    {
      return new StreamResponse(() => bundle.Assets[0].OpenStream(), bundle.ContentType);
    }

    private Response SetCacheHeaders(Response response, string actualETag)
    {
      response.WithHeader("Cache-Control", "public");
      response.WithHeader("ETag", actualETag);
      response.WithHeader("Expires", DateTime.UtcNow.AddYears(1).ToString("R"));
      return response;
    }

    private static string GetBundleETag(Bundle bundle)
    {
      return string.Concat("\"", ToHexString(bundle.Hash), "\"");
    }

    public static string ToHexString(IEnumerable<byte> bytes)
    {
        return string.Concat(bytes.Select(b => b.ToString("x2")).ToArray());
    }
  }
}