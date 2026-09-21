using System.Collections.Generic;

namespace WebFramework.Localization;

/// <summary>
/// Central localization adapter for the ERP UI layer.
/// <para>
/// Two lookup styles are supported deliberately:
/// </para>
/// <list type="bullet">
/// <item><description>
/// <see cref="T(string, string?)"/> — key based, used by the new shared shell,
/// templates and TagHelpers. The key lives in <c>ErpUi.resx</c>.
/// </description></item>
/// <item><description>
/// <see cref="Dynamic(string?)"/> — value based, used for metadata that is
/// already stored as Persian text in <c>ControllerInfoAttribute</c>,
/// <c>ActionDisplayNameAttribute</c>, <c>Display</c>/<c>DisplayName</c>
/// attributes, dynamic menu records and data-profile titles. The existing
/// Persian string acts as the resource key, so no entity or database row has
/// to be rewritten to translate the UI. When no translation exists the source
/// text is returned unchanged.
/// </description></item>
/// </list>
/// </summary>
public interface IErpUiLocalizer
{
	/// <summary>Current request culture name, e.g. <c>fa-IR</c>.</summary>
	string Culture { get; }

	/// <summary>Text direction for the current culture: <c>rtl</c> or <c>ltr</c>.</summary>
	string Direction { get; }

	/// <summary>True when the current culture is right-to-left.</summary>
	bool IsRtl { get; }

	/// <summary>Calendar the UI should present for the current culture.</summary>
	string Calendar { get; }

	/// <summary>Key-based lookup. Returns <paramref name="fallback"/> (or the key) when missing.</summary>
	string T(string key, string? fallback = null);

	/// <summary>Key-based lookup with <c>{name}</c> placeholder substitution.</summary>
	string T(string key, string? fallback, IReadOnlyDictionary<string, object?> parameters);

	/// <summary>
	/// Fallback-key lookup for dynamic Persian metadata. Returns
	/// <paramref name="sourceText"/> unchanged when no translation is registered.
	/// </summary>
	string Dynamic(string? sourceText);

	/// <summary>The string catalog handed to the browser once per full page load.</summary>
	IReadOnlyDictionary<string, string> ClientCatalog();
}
