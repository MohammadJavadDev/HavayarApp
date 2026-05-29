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
			string? template= null ,
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

			// 2. جمع‌آوری اطلاعات ستون‌های جستجو و نوع آنها
			var searchColumnInfo = new List<SearchColumnInfo>();
			if (searchColumns != null)
			{
				searchColumnInfo = GetSearchColumnInfo<T>(searchColumns, projection);
			}

			// 3. تولید SQL (استفاده از SqlUtils)
			string rawSql = query.Select(projection).ToQueryString();
			var (cleanSql, sqlParams) = SqlUtils.ParseAndClean(rawSql);

			// 4. تزریق ID
			var projectionProps = ParseProperties(projection).ToList();
			bool hasId = projectionProps.Contains("Id", StringComparer.OrdinalIgnoreCase);

			cleanSql = SqlUtils.InjectIdIfMissing(cleanSql, hasId, out _);
			if (!hasId) projectionProps.Insert(0, "Id");

			var definition = new SelectorDefinition
			{
				Sql = cleanSql,
				ColMap = projectionProps.ToArray(),
				DisplayTemplate = displayTemplate,
				SearchCols = ParsePropertyNames(searchColumns),
				SearchColumnInfos = searchColumnInfo
			};

			// 5. کش و رمزنگاری
			string queryId = sqlStore.GetOrSet(definition);
			string encryptedParams = protector.Protect(JsonSerializer.Serialize(sqlParams));

			// ذخیره اطلاعات نوع ستون‌ها برای استفاده در سرویس
			string searchColumnsInfo = JsonSerializer.Serialize(searchColumnInfo);
			string encryptedSearchInfo = protector.Protect(searchColumnsInfo);

			// 6. SSR: دریافت دیتای اولیه (توسط سرویس)
			var initialItems = new List<EntitySelectorItem>();
			if (!string.IsNullOrEmpty(selectedId))
			{
				var ids = selectedId.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
				if (ids.Any())
				{
					initialItems = selectorService.GetInitialItemsSync(ids, cleanSql, sqlParams, displayTemplate, projectionProps.ToArray());
				
				}
			}
			string initialNamesJoined = string.Join(" , ", initialItems.Select(x => x.Display));


			// 7. ساخت UI
			var container = new TagBuilder("div");
			container.AddCssClass("entity-selector-wrapper position-relative");
			container.Attributes.Add("id", id);
			container.Attributes.Add("data-entity", typeof(T).Name);
			container.Attributes.Add("data-query-id", queryId);
			container.Attributes.Add("data-default-params", encryptedParams);
			container.Attributes.Add("data-search-columns-info", encryptedSearchInfo); // اطلاعات جدید

			container.Attributes.Add("data-page-size", pageSize.ToString());
			container.Attributes.Add("data-api-url", "/api/entity-selector");
			if (multiSelect) container.Attributes.Add("data-multi-select", "true");
			if (_readonly) container.Attributes.Add("data-readonly", "true");
			if (template.HasValue()) container.Attributes.Add("data-template", template);

			// دیتای اولیه برای JS
			var jsInitData = initialItems.Select(x => new { id = x.Id, display = x.Display , row = x.Row }).ToList();

			var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
			container.Attributes.Add("data-initial-items", JsonSerializer.Serialize(jsInitData, jsonOptions));
 

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
				var readonlySpan = new TagBuilder("span");
				readonlySpan.AddCssClass("form-control-plaintext border p-3 border-radius");
				if (multiSelect && initialItems.Any())
				{
					foreach(var item in initialItems.Select(x => $"<span class='entity-chip badge badge-success'>{x.Display}</span>"))
				{
					readonlySpan.InnerHtml.AppendHtml(item);
				}
				 
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

		/// <summary>
		/// فقط برای string ها predicate می‌سازد (برای تولید SQL اولیه)
		/// </summary>
		private static Expression<Func<T, bool>> BuildSearchPredicateStringOnly<T>(Expression<Func<T, object>> searchCols, string token)
		{
			var param = Expression.Parameter(typeof(T), "x");
			Expression body = null;
			var stringContainsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) });
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
					var notNullCheck = Expression.NotEqual(prop, Expression.Constant(null, typeof(string)));
					var containsCall = Expression.Call(prop, stringContainsMethod, tokenExpr);
					var searchExpression = Expression.AndAlso(notNullCheck, containsCall);
					body = body == null ? searchExpression : Expression.OrElse(body, searchExpression);
				}
			}
			return body == null ? null : Expression.Lambda<Func<T, bool>>(body, param);
		}

		/// <summary>
		/// جمع‌آوری اطلاعات ستون‌های جستجو با نوع آنها
		/// </summary>
		private static List<SearchColumnInfo> GetSearchColumnInfo<T>(
						    Expression<Func<T, object>> searchCols,
						    Expression<Func<T, object>> projection)   // ← پارامتر جدید
		{
			var result = new List<SearchColumnInfo>();
			var param = Expression.Parameter(typeof(T), "x");

			// نگاشت: "ProductionOrder.ProductionOrderNumber" → "ProductionOrderNumber"
			var projectionMap = BuildProjectionMap<T>(projection);

			foreach (var propPath in GetPropertyPaths(searchCols))
			{
				Expression prop = param;
				foreach (var member in propPath.Split('.'))
					prop = Expression.Property(prop, member);

				var propType = Nullable.GetUnderlyingType(prop.Type) ?? prop.Type;

				// پیدا کردن alias واقعی در SQL
				string sqlAlias = projectionMap.TryGetValue(propPath, out string alias)
				    ? alias
				    : propPath.Split('.').Last(); // fallback: آخرین بخش مسیر

				result.Add(new SearchColumnInfo
				{
					ColumnName = propPath,
					SqlAlias = sqlAlias,       // ← نام واقعی ستون در CTE
					TypeName = propType.Name,
					IsString = propType == typeof(string)
				});
			}

			return result;
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

		private static Dictionary<string, string> BuildProjectionMap<T>(Expression<Func<T, object>> projection)
		{
			var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

			var body = projection.Body;
			if (body is UnaryExpression unary) body = unary.Operand;

			if (body is NewExpression newExpr)
			{
				for (int i = 0; i < newExpr.Arguments.Count; i++)
				{
					string alias = newExpr.Members[i].Name;
					string path = GetFullPropertyName(newExpr.Arguments[i]);
					if (!string.IsNullOrEmpty(path))
						map[path] = alias;
				}
			}

			return map;
		}

		private static string ParsePropertyNames(Expression expr) => string.Join(",", GetPropertyPaths(expr));
	}

	 
}