﻿using System;
using Cassette;
using Cassette.Utilities;

namespace Nancy.Cassette
{
  public class UrlAndPathGenerator : IUrlGenerator
  {
    public static string AssetUrlPrefix = "_assets";
    
    public string CreateModuleUrl(Module module)
    {
      return string.Format(
        "/{0}/{1}/{2}_{3}",
        AssetUrlPrefix,
        ConventionalModulePathName(module.GetType()),
        module.Path.Substring(2),
        module.Assets[0].Hash.ToHexString()
        );
    }

    public string CreateAssetUrl(IAsset asset)
    {
      return string.Format(
        "/{0}?{1}",
        asset.SourceFilename.Substring(2),
        asset.Hash.ToHexString()
        );
    }

    public string CreateAssetCompileUrl(Module module, IAsset asset)
    {
      return string.Format(
        "/{0}/get/{1}?{2}",
        AssetUrlPrefix,
        asset.SourceFilename.Substring(2),
        asset.Hash.ToHexString()
        );
    }

    public string CreateRawFileUrl(string filename, string hash)
    {
      if (filename.StartsWith("~") == false)
      {
        throw new ArgumentException("Filename must be application relative (starting with '~').");
      }

      filename = filename.Substring(2); // Remove the "~/"
      var dotIndex = filename.LastIndexOf('.');
      var name = filename.Substring(0, dotIndex);
      var extension = filename.Substring(dotIndex + 1);

      return string.Format("/{0}/files/{1}_{2}_{3}",
                           AssetUrlPrefix,
                           ConvertToForwardSlashes(name),
                           hash,
                           extension
        );
    }


    public static string GetModuleHandlerRoot<T>()
    {
      return string.Format(
        "/{0}/{1}",
        AssetUrlPrefix,
        ConventionalModulePathName(typeof (T)));
    }


    public static string GetRawFileHandlerRoot()
    {
      return string.Format(
        "/{0}/files",
        AssetUrlPrefix);
    }

    public static string GetCompiledAssetHandlerRoot()
    {
      return string.Format(
        "/{0}/get",
        AssetUrlPrefix);
    }

    public static bool RemoveUrlQuery(string url, out string urlWithoutQuery, out string query)
    {
      var queryPosition = url.IndexOf('?');
      if (queryPosition >= 0)
      {
        urlWithoutQuery = url.Remove(queryPosition);
        query = url.Substring(queryPosition + 1);
        return true;
      }

      urlWithoutQuery = url;
      query = null;
      return false;
    }

    private static string ConventionalModulePathName(Type moduleType)
    {
      // ExternalScriptModule subclasses ScriptModule, but we want the name to still be "scripts"
      // So walk up the inheritance chain until we get to something that directly inherits from Module.
      while (moduleType.BaseType != typeof (Module))
      {
        moduleType = moduleType.BaseType;
      }

      var name = moduleType.Name;
      name = name.Substring(0, name.Length - "Module".Length);
      return name.ToLowerInvariant() + "s";
    }

    private static string ConvertToForwardSlashes(string path)
    {
      return path.Replace('\\', '/');
    }
  }
}