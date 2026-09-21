using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Resources;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

namespace WebFramework.Localization;

/// <summary>
/// Default <see cref="IErpUiLocalizer"/>. Reads the two .resx catalogs through
/// <see cref="ResourceManager"/> so satellite assemblies and culture fallback
/// behave the way ASP.NET Core expects, and caches the per-culture client
/// catalog because it is serialized into every full page load.
/// </summary>
public sealed class ErpUiLocalizer : IErpUiLocalizer
{
	private static readonly Regex PlaceholderPattern =
		new(@"\{(\w+)\}", RegexOptions.Compiled | RegexOptions.CultureInvariant);

	private static readonly ConcurrentDictionary<string, IReadOnlyDictionary<string, string>> ClientCatalogCache = new();

	private readonly ErpLocalizationOptions _options;
	private readonly ResourceManager? _uiResources;
	private readonly ResourceManager? _dynamicResources;

	public ErpUiLocalizer(IOptions<ErpLocalizationOptions> options)
	{
		_options = options.Value;
		_uiResources = TryCreateResourceManager(_options.UiResourceBaseName);
		_dynamicResources = TryCreateResourceManager(_options.DynamicResourceBaseName);
	}

	private ResourceManager? TryCreateResourceManager(string baseName)
	{
		try
		{
			var assembly = AppDomain.CurrentDomain
				.GetAssemblies()
				.FirstOrDefault(a => string.Equals(a.GetName().Name, _options.ResourceAssemblyName, StringComparison.OrdinalIgnoreCase));

			if (assembly == null)
				return null;

			// Resources live under <Assembly>/Resources/<BaseName>.resx, which the
			// SDK embeds as "<RootNamespace>.Resources.<BaseName>".
			return new ResourceManager($"{_options.ResourceAssemblyName}.Resources.{baseName}", assembly);
		}
		catch
		{
			// A missing catalog must never break rendering; fallbacks cover it.
			return null;
		}
	}

	public string Culture => CultureInfo.CurrentUICulture.Name;

	public bool IsRtl
	{
		get
		{
			var name = CultureInfo.CurrentUICulture.Name;
			var twoLetter = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
			return _options.RightToLeftCultures.Any(c =>
				string.Equals(c, name, StringComparison.OrdinalIgnoreCase) ||
				string.Equals(c, twoLetter, StringComparison.OrdinalIgnoreCase));
		}
	}

	public string Direction => IsRtl ? "rtl" : "ltr";

	public string Calendar =>
		CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals("fa", StringComparison.OrdinalIgnoreCase)
			? "persian"
			: "gregorian";

	public string T(string key, string? fallback = null)
	{
		if (string.IsNullOrEmpty(key))
			return fallback ?? string.Empty;

		var value = Lookup(_uiResources, key);
		if (!string.IsNullOrEmpty(value))
			return value!;

		return fallback ?? key;
	}

	public string T(string key, string? fallback, IReadOnlyDictionary<string, object?> parameters)
	{
		var text = T(key, fallback);
		if (parameters == null || parameters.Count == 0)
			return text;

		return PlaceholderPattern.Replace(text, match =>
			parameters.TryGetValue(match.Groups[1].Value, out var replacement)
				? replacement?.ToString() ?? string.Empty
				: match.Value);
	}

	public string Dynamic(string? sourceText)
	{
		if (string.IsNullOrWhiteSpace(sourceText))
			return string.Empty;

		// The Persian source text is the resource key. Trim only for lookup so
		// keys with trailing spaces in the attributes still resolve.
		var value = Lookup(_dynamicResources, sourceText.Trim());
		return string.IsNullOrEmpty(value) ? sourceText : value!;
	}

	private static string? Lookup(ResourceManager? manager, string key)
	{
		if (manager == null)
			return null;

		try
		{
			return manager.GetString(key, CultureInfo.CurrentUICulture);
		}
		catch (MissingManifestResourceException)
		{
			return null;
		}
		catch (InvalidOperationException)
		{
			return null;
		}
	}

	public IReadOnlyDictionary<string, string> ClientCatalog()
	{
		var culture = CultureInfo.CurrentUICulture.Name;
		return ClientCatalogCache.GetOrAdd(culture, _ => BuildClientCatalog());
	}

	private IReadOnlyDictionary<string, string> BuildClientCatalog()
	{
		var result = new Dictionary<string, string>(StringComparer.Ordinal);
		if (_uiResources == null)
			return result;

		try
		{
			var set = _uiResources.GetResourceSet(CultureInfo.CurrentUICulture, createIfNotExists: true, tryParents: true);
			if (set == null)
				return result;

			foreach (System.Collections.DictionaryEntry entry in set)
			{
				if (entry.Key is string key && entry.Value is string value)
					result[key] = value;
			}
		}
		catch (MissingManifestResourceException)
		{
			// No catalog compiled: the client falls back to inline defaults.
		}

		return result;
	}
}
