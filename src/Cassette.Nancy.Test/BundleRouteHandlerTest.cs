using System;
using System.Linq;
using System.Reflection;
using HtmlAgilityPlus;
using NUnit.Framework;
using Nancy;
using Nancy.Testing;

namespace Cassette.Nancy.Test
{
  public class BundleRouteHandlerTest
  {
    [TestCase("head", "link", "href", "/_cassette/stylesheetbundle/Styles")]
    [TestCase("body", "script", "src", "/_cassette/scriptbundle/Scripts/lib")]
    [TestCase("body", "script", "src", "/_cassette/scriptbundle/Scripts/app")]
    public void BundleIsReturned(string location, string element, string attribute, string urlFragmet)
    {
      var browser = new Browser(new OptimizingBootstrapper());
      var response = browser.Get("/RazorHome", with => with.HttpRequest());
      Console.Write(response.Body.AsString());
      
      var query = new SharpQuery(response.Body.AsString());

      var url = query.Find(string.Format("{0} {1}[{2}^='{3}']", location, element, attribute, urlFragmet)).Attr(attribute);
      
      response = browser.Get(url, with => with.HttpRequest());
      Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
      Console.Write(response.Body.AsString());
    }

    [TestCase("~/Styles")]
    [TestCase("~/Scripts/lib")]
    [TestCase("~/Scripts/app")]
    public void CacheHeaderAndETagAreSet(string bundlePath)
    {
      var browser = new Browser(new OptimizingBootstrapper());
      var response = browser.Get("/RazorHome", with => with.HttpRequest());

      var bundle = CassetteApplicationContainer.Application.Bundles.First(b => b.Path.Equals(bundlePath, StringComparison.OrdinalIgnoreCase));

      // url is internal but don't want to duplicate the logic so use reflection
      var bundleUrl = bundle.GetType()
        .GetProperty("Url", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
        .GetValue(bundle, null).ToString();
      var url = string.Format("/_cassette/{0}", bundleUrl);

      response = browser.Get(url, with => with.HttpRequest());

      Assert.AreEqual("public", response.Headers["Cache-Control"]);
      Assert.AreEqual(GetBundleETag(bundle), response.Headers["ETag"]);
      Assert.AreEqual(DateTime.UtcNow.AddYears(1).ToString("R"), response.Headers["Expires"]);
    }

    [TestCase("~/Styles")]
    [TestCase("~/Scripts/lib")]
    [TestCase("~/Scripts/app")]
    public void WhenRequestingWithCurrentETag_304ResponseIsReturned(string bundlePath)
    {
      var browser = new Browser(new OptimizingBootstrapper());
      var response = browser.Get("/RazorHome", with => with.HttpRequest());

      var bundle = CassetteApplicationContainer.Application.Bundles.First(b => b.Path.Equals(bundlePath, StringComparison.OrdinalIgnoreCase));

      // url is internal but don't want to duplicate the logic so use reflection
      var bundleUrl = bundle.GetType()
        .GetProperty("Url", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
        .GetValue(bundle, null).ToString();
      var url = string.Format("/_cassette/{0}", bundleUrl);

      response = browser.Get(url, with =>
      {
        with.HttpRequest();
        with.Header("If-None-Match", GetBundleETag(bundle));
      });

      Assert.AreEqual(HttpStatusCode.NotModified, response.StatusCode);
      Assert.IsEmpty(response.Body.AsString());

      Assert.AreEqual("public", response.Headers["Cache-Control"]);
      Assert.AreEqual(@"""" + string.Concat(bundle.Hash.Select(b => b.ToString("x2")).ToArray()) + @"""", response.Headers["ETag"]);
      Assert.AreEqual(DateTime.UtcNow.AddYears(1).ToString("R"), response.Headers["Expires"]);
    }

    [TestCase("~/Styles")]
    [TestCase("~/Scripts/lib")]
    [TestCase("~/Scripts/app")]
    public void WhenRequestingWithOldETag_BundleIsReturned(string bundlePath)
    {
      var browser = new Browser(new OptimizingBootstrapper());
      var response = browser.Get("/RazorHome", with => with.HttpRequest());

      var bundle = CassetteApplicationContainer.Application.Bundles.First(b => b.Path.Equals(bundlePath, StringComparison.OrdinalIgnoreCase));

      // url is internal but don't want to duplicate the logic so use reflection
      var bundleUrl = bundle.GetType()
        .GetProperty("Url", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
        .GetValue(bundle, null).ToString();
      var url = string.Format("/_cassette/{0}", bundleUrl);

      response = browser.Get(url, with =>
      {
        with.HttpRequest();
        with.Header("If-None-Match", @"""old_etag""");
      });

      Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
      Assert.AreEqual("public", response.Headers["Cache-Control"]);
      Assert.AreEqual(@"""" + string.Concat(bundle.Hash.Select(b => b.ToString("x2")).ToArray()) + @"""", response.Headers["ETag"]);
      Assert.AreEqual(DateTime.UtcNow.AddYears(1).ToString("R"), response.Headers["Expires"]);
      // todo assert contents
    }

    private static string GetBundleETag(Bundle bundle)
    {
      return @"""" + ToHexString(bundle.Hash) + @"""";
    }

    private static string ToHexString(byte[] bytes)
    {
      return string.Concat(bytes.Select(b => b.ToString("x2")).ToArray());
    }
  }
}