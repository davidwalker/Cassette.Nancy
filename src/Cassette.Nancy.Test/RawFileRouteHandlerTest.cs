using System;
using System.IO;
using System.Security.Cryptography;
using NUnit.Framework;
using Nancy;
using Nancy.Testing;
using Nancy.Testing.Fakes;

namespace Cassette.Nancy.Test
{
  public class RawFileRouteHandlerTest
  {
    [TestCase("/_cassette/file/Styles/images/lorry_cffc46f6f108699377f0d4f92e88be78e31e5fcc_png")]
    public void RawImageFileIsReturned(string url)
    {
      var browser = new Browser(new NonOptimizingBootstrapper());
      var response = browser.Get(url, with => with.HttpRequest());
      Console.Write(response.Body.AsString());
      
      Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [Test]
    public void CacheHeadersAreSet()
    {
      var browser = new Browser(new NonOptimizingBootstrapper());

      var url = "/_cassette/file/Styles/images/lorry_cffc46f6f108699377f0d4f92e88be78e31e5fcc_png";
      string realFilePath = Path.Combine(FakeRootPathProvider.RootPath, @"styles\images\lorry.png");

      var response = browser.Get(url, with => with.HttpRequest());
      
      Assert.AreEqual("public", response.Headers["Cache-Control"]);
      Assert.AreEqual(GetETag(realFilePath), response.Headers["ETag"]);
      Assert.AreEqual(DateTime.UtcNow.AddYears(1).ToString("R"), response.Headers["Expires"]);
    }
    [Test]
    public void RequestWithCurrentETag_304IsReturned()
    {
      var browser = new Browser(new NonOptimizingBootstrapper());

      var url = "/_cassette/file/Styles/images/lorry_cffc46f6f108699377f0d4f92e88be78e31e5fcc_png";
      string realFilePath = Path.Combine(FakeRootPathProvider.RootPath, @"styles\images\lorry.png");

      var response = browser.Get(url, with =>
      {
        with.HttpRequest();
        with.Header("If-None-Match", GetETag(realFilePath));
      });

      Assert.AreEqual(HttpStatusCode.NotModified, response.StatusCode);
      Assert.IsEmpty(response.Body.AsString());
      
      Assert.AreEqual("public", response.Headers["Cache-Control"]);
      Assert.AreEqual(GetETag(realFilePath), response.Headers["ETag"]);
      Assert.AreEqual(DateTime.UtcNow.AddYears(1).ToString("R"), response.Headers["Expires"]);
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

  }
}