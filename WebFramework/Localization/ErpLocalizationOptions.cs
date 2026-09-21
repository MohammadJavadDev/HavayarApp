namespace WebFramework.Localization;

/// <summary>
/// Where the ERP UI resource files live and which cultures the shell offers.
/// Defaults match the repository layout: <c>WebApp/Resources/ErpUi.*.resx</c>.
/// </summary>
public sealed class ErpLocalizationOptions
{
	/// <summary>Assembly (and root namespace) that owns the .resx files.</summary>
	public string ResourceAssemblyName { get; set; } = "WebApp";

	/// <summary>Base name of the shared UI string catalog.</summary>
	public string UiResourceBaseName { get; set; } = "ErpUi";

	/// <summary>
	/// Base name of the fallback-key catalog used for Persian metadata that is
	/// stored in attributes and database rows.
	/// </summary>
	public string DynamicResourceBaseName { get; set; } = "ErpDynamic";

	/// <summary>Default culture when the request carries no preference.</summary>
	public string DefaultCulture { get; set; } = "fa-IR";

	/// <summary>Cultures the language switcher exposes.</summary>
	public string[] SupportedCultures { get; set; } = ["fa-IR", "en-US"];

	/// <summary>Cultures rendered right-to-left.</summary>
	public string[] RightToLeftCultures { get; set; } = ["fa", "ar", "he", "fa-IR"];
}
