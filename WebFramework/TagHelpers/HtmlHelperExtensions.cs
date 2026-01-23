
using Common.Entities;
using Common.Utilities;
using Data;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Services;
using Services.InMemoryData;
using System;
using System.Data;

using System.Linq.Expressions;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace WebFramework.TagHelpers
{
	public static class HtmlHelperExtensions
	{
		private const string SEARCH_TOKEN = "***SEARCH*TOKEN***";


		public static IHtmlContent EntitySelector<T>(
			this IHtmlHelper html,
			string id,
			Expression<Func<T, bool>> filter,
			Expression<Func<T, object>> projection,
			Expression<Func<T, object>> searchColumns,
			string displayTemplate,
			string bindProperty = null,
			string bindNameProperty = null,
			string? selectedId = null,
			bool required = false,
			bool multiSelect = false,
			bool disabled = false,
			bool _readonly = false,
			string placeholder = "جستجو...",
			int pageSize = 20) where T : class
		{
			var services = html.ViewContext.HttpContext.RequestServices;
			var sqlStore = services.GetRequiredService<SqlCacheStore>();
			var dbContext = services.GetRequiredService<ApplicationDbContext>();
			var protector = services.GetRequiredService<PayloadProtector>();

			var selectorService = services.GetRequiredService<IEntitySelectorService>();

			// 1. ساخت کوئری
			var query = dbContext.Set<T>().AsQueryable();
			if (filter != null) query = query.Where(filter);
			if (searchColumns != null)
			{
				var searchPredicate = BuildSearchPredicate<T>(searchColumns, SEARCH_TOKEN);
				if (searchPredicate != null) query = query.Where(searchPredicate);
			}

			// 2. تولید SQL (استفاده از SqlUtils)
			string rawSql = query.Select(projection).ToQueryString();
			var (cleanSql, sqlParams) = SqlUtils.ParseAndClean(rawSql);

			// 3. تزریق ID
			var projectionProps = ParseProperties(projection).ToList();
			bool hasId = projectionProps.Contains("Id", StringComparer.OrdinalIgnoreCase);

			cleanSql = SqlUtils.InjectIdIfMissing(cleanSql, hasId, out _);
			if (!hasId) projectionProps.Insert(0, "Id");

			var definition = new SelectorDefinition
			{
				Sql = cleanSql,
				ColMap = projectionProps.ToArray(),
				DisplayTemplate = displayTemplate,
				SearchCols = ParsePropertyNames(searchColumns)
			};

			// 4. کش و رمزنگاری
			string queryId = sqlStore.GetOrSet(definition);
			string encryptedParams = protector.Protect(JsonSerializer.Serialize(sqlParams));
			string colMapStr = string.Join(",", projectionProps);

			// 5. SSR: دریافت دیتای اولیه (توسط سرویس)
			var initialItems = new List<EntitySelectorItem>();
			if (!string.IsNullOrEmpty(selectedId))
			{
				var ids = selectedId.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
				if (ids.Any())
				{
					// فراخوانی متد سرویس برای اجرای SQL
					initialItems = selectorService.GetInitialItemsSync(ids, cleanSql, sqlParams, displayTemplate, projectionProps.ToArray());
				}
			}
			string initialNamesJoined = string.Join(" , ", initialItems.Select(x => x.Display));


			// 6. ساخت UI
			var container = new TagBuilder("div");
			container.AddCssClass("entity-selector-wrapper position-relative");
			container.Attributes.Add("id", id);
			container.Attributes.Add("data-entity", typeof(T).Name);
			container.Attributes.Add("data-query-id", queryId);
			container.Attributes.Add("data-default-params", encryptedParams);

			container.Attributes.Add("data-page-size", pageSize.ToString());
			container.Attributes.Add("data-api-url", "/api/entity-selector");
			if (multiSelect) container.Attributes.Add("data-multi-select", "true");
			if (_readonly) container.Attributes.Add("data-readonly", "true");

			// دیتای اولیه برای JS
			var jsInitData = initialItems.Select(x => new { id = x.Id, display = x.Display }).ToList();
			container.Attributes.Add("data-initial-items", JsonSerializer.Serialize(jsInitData));

			// Hidden Inputs
			var hidden = new TagBuilder("input");
			hidden.Attributes.Add("type", "hidden");
			hidden.Attributes.Add("name", id);
			hidden.AddCssClass("entity-selector-value");
			hidden.Attributes.Add("value", selectedId);
			hidden.Attributes.Add("data-entity-selector", "true");
			if (required)
				hidden.Attributes.Add("required", "required");
			if (!string.IsNullOrEmpty(bindProperty)) hidden.Attributes.Add("data-bind", bindProperty);

			if (!string.IsNullOrEmpty(bindNameProperty))
			{
				var hName = new TagBuilder("input");
				hName.Attributes.Add("type", "hidden");
				hName.AddCssClass("entity-selector-text");
				hName.Attributes.Add("data-bind", bindNameProperty);
				hName.Attributes.Add("value", initialNamesJoined);

				container.InnerHtml.AppendHtml(hName);
			}
			container.InnerHtml.AppendHtml(hidden);

			// UI Elements
			if (_readonly)
			{
				// Readonly Mode: Display as span
				var readonlySpan = new TagBuilder("span");
				readonlySpan.AddCssClass("form-control-plaintext");
				if (multiSelect && initialItems.Any())
				{
					readonlySpan.InnerHtml.AppendHtml(string.Join(" , ", initialItems.Select(x => x.Display)));
				}
				else if (!multiSelect && initialItems.Any())
				{
					readonlySpan.InnerHtml.AppendHtml(initialItems[0].Display);
				}
				else
				{
					readonlySpan.InnerHtml.AppendHtml("-");
				}
				container.InnerHtml.AppendHtml(readonlySpan);
			}
			else
			{
				// Normal or Disabled Mode
				var inputGroup = new TagBuilder("div");
				var input = new TagBuilder("input");
				input.Attributes.Add("type", "text");
				input.Attributes.Add("placeholder", placeholder);
				input.Attributes.Add("autocomplete", "off");

				if (disabled)
				{
					input.Attributes.Add("disabled", "disabled");
				}

				if (multiSelect)
				{
					// Multi-Select UI (Chips Rendered by Server)
					var multiContainer = new TagBuilder("div");
					multiContainer.AddCssClass("entity-selector-multi-container");
					foreach (var item in initialItems)
					{
						var chip = new TagBuilder("span");
						chip.AddCssClass("entity-chip badge bg-primary");
						chip.InnerHtml.AppendHtml($"{item.Display} <span class='remove-chip' title='حذف'>&times;</span>");
						multiContainer.InnerHtml.AppendHtml(chip);
					}
					input.AddCssClass("entity-selector-multi-input");
					multiContainer.InnerHtml.AppendHtml(input);
					multiContainer.InnerHtml.AppendHtml(BuildIcons());

					container.InnerHtml.AppendHtml(multiContainer);

					inputGroup.AddCssClass("input-group");
					inputGroup.Attributes.Add("style", "display:none");
					inputGroup.InnerHtml.AppendHtml(new TagBuilder("input"));
					container.InnerHtml.AppendHtml(inputGroup);
				}
				else
				{
					// Single-Select UI
					input.AddCssClass("form-control entity-selector-input");
					if (initialItems.Any()) input.Attributes.Add("value", initialItems[0].Display);

					inputGroup.AddCssClass("input-group");
					inputGroup.InnerHtml.AppendHtml(input);
					inputGroup.InnerHtml.AppendHtml(BuildIcons());
					container.InnerHtml.AppendHtml(inputGroup);
				}

				var dropdown = new TagBuilder("div");
				dropdown.AddCssClass("dropdown-menu   entity-selector-results");
				dropdown.Attributes.Add("style", "max-height: 300px; overflow-y: auto;");

				container.InnerHtml.AppendHtml(dropdown);
			}


			return container;
		}

		private static IHtmlContent BuildIcons()
		{
			var d = new TagBuilder("div"); d.AddCssClass("entity-icons");
			var a = new TagBuilder("span"); a.AddCssClass("entity-arrow"); a.InnerHtml.AppendHtml("&#9662;");
			var c = new TagBuilder("span"); c.AddCssClass("entity-clear"); c.Attributes.Add("style", "display:none;"); c.InnerHtml.AppendHtml("&times;");
			d.InnerHtml.AppendHtml(c); d.InnerHtml.AppendHtml(a);
			return d;
		}

		private static Expression<Func<T, bool>> BuildSearchPredicate<T>(Expression<Func<T, object>> searchCols, string token)
		{
			var param = Expression.Parameter(typeof(T), "x");
			Expression body = null;
			var containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) });
			var tokenExpr = Expression.Constant(token);
			foreach (var propPath in GetPropertyPaths(searchCols))
			{
				Expression prop = param;
				foreach (var member in propPath.Split('.'))
				{
					prop = Expression.Property(prop, member);
				}

				if (prop.Type == typeof(string))
				{
					var call = Expression.Call(prop, containsMethod, tokenExpr);
					body = body == null ? call : Expression.OrElse(body, call);
				}
			}
			return body == null ? null : Expression.Lambda<Func<T, bool>>(body, param);
		}

		private static IEnumerable<string> GetPropertyPaths(Expression expr)
		{
			if (expr is LambdaExpression lambda) expr = lambda.Body;
			if (expr is UnaryExpression unary) expr = unary.Operand;
			if (expr is NewExpression newExpr)
			{
				return newExpr.Arguments.Select(GetFullPropertyName);
			}
			if (expr is MemberExpression memberExpr)
			{
				return new[] { GetFullPropertyName(memberExpr) };
			}
			return Array.Empty<string>();
		}

		private static string GetFullPropertyName(Expression expr)
		{
			if (expr is UnaryExpression unary) expr = unary.Operand;
			if (expr is MemberExpression memberExpr)
			{
				var parent = GetFullPropertyName(memberExpr.Expression);
				return string.IsNullOrEmpty(parent) ? memberExpr.Member.Name : parent + "." + memberExpr.Member.Name;
			}
			return "";
		}

		private static string[] ParseProperties(Expression expr)
		{
			if (expr is LambdaExpression lambda) expr = lambda.Body;
			if (expr is UnaryExpression unary) expr = unary.Operand;
			if (expr is NewExpression newExpr) return newExpr.Members.Select(m => m.Name).ToArray();
			if (expr is MemberExpression memberExpr) return new[] { memberExpr.Member.Name };
			return Array.Empty<string>();
		}
		private static string ParsePropertyNames(Expression expr) => string.Join(",", GetPropertyPaths(expr));
	}
}