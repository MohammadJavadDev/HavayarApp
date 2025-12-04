/*
 * ========================================================================
 * DYNAMIC EXPRESSION BUILDER - USAGE EXAMPLES
 * ========================================================================
 * 
 * This file contains comprehensive examples of how to use the 
 * DynamicExpressionBuilder with various field types and operators.
 * 
 * NOTE: This file is for documentation purposes only and should not be
 * compiled into the production build. It serves as a reference guide.
 * ========================================================================
 */

#if DOCUMENTATION_ONLY

using Common.Entities.EntityMetadatas;
using Entities.Base.NotifitactionBuilder;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Services.NotifitactionBuilderServices.Examples
{
	/// <summary>
	/// Example entity for demonstration purposes
	/// </summary>
	public class Contact
	{
		public long Id { get; set; }
		public string NationalCode { get; set; }
		public string FirstName { get; set; }
		public string LastName { get; set; }
		public DateTime CreatedOn { get; set; }
		public DateTime? ModifiedDate { get; set; }
		public int Status { get; set; }
		public bool IsActive { get; set; }
		public List<Address> Addresses { get; set; }
		public Company Company { get; set; }
	}

	public class Address
	{
		public long Id { get; set; }
		public string Street { get; set; }
		public int Type { get; set; }
		public City City { get; set; }
	}

	public class City
	{
		public long Id { get; set; }
		public string Name { get; set; }
	}

	public class Company
	{
		public long Id { get; set; }
		public string Name { get; set; }
		public int EmployeeCount { get; set; }
	}

	/// <summary>
	/// Comprehensive usage examples for DynamicExpressionBuilder
	/// </summary>
	public class ExpressionBuilderExamples
	{
		// ========================================================================
		// EXAMPLE 1: Simple String Condition with Contains
		// ========================================================================
		public void Example1_SimpleStringContains()
		{
			/*
			 * JSON Structure:
			 * {
			 *   "Logic": "AND",
			 *   "Criteria": [
			 *     {
			 *       "Data": "NationalCode",
			 *       "Condition": "contains",
			 *       "Value": ["1100"],
			 *       "FieldType": "string"
			 *     }
			 *   ]
			 * }
			 */

			var rule = new RolesNode
			{
				Logic = "AND",
				Criteria = new List<RolesNode>
				{
					new RolesNode
					{
						Data = "NationalCode",
						Condition = "contains",
						Value = new List<string> { "1100" },
						FieldType = "string"
					}
				}
			};

			var entityMetadata = CreateContactMetadata();
			var expression = DynamicExpressionBuilder.BuildExpression<Contact>(rule, entityMetadata);

			// Usage with EF Core:
			// var results = context.Contacts.Where(expression).ToList();
			
			// Generated Expression: x => x.NationalCode != null && x.NationalCode.Contains("1100")
		}

		// ========================================================================
		// EXAMPLE 2: Nested Entity Navigation (Entity Type)
		// ========================================================================
		public void Example2_NestedEntityNavigation()
		{
			/*
			 * JSON Structure:
			 * {
			 *   "Logic": "AND",
			 *   "Criteria": [
			 *     {
			 *       "Data": "Company",
			 *       "Condition": "=",
			 *       "SubField": {
			 *         "Data": "EmployeeCount",
			 *         "Condition": ">",
			 *         "Value": ["100"],
			 *         "FieldType": "int"
			 *       },
			 *       "FieldType": "entity"
			 *     }
			 *   ]
			 * }
			 */

			var rule = new RolesNode
			{
				Logic = "AND",
				Criteria = new List<RolesNode>
				{
					new RolesNode
					{
						Data = "Company",
						Condition = "=",
						SubField = new RolesNode
						{
							Data = "EmployeeCount",
							Condition = ">",
							Value = new List<string> { "100" },
							FieldType = "int"
						},
						FieldType = "entity"
					}
				}
			};

			var entityMetadata = CreateContactMetadata();
			var expression = DynamicExpressionBuilder.BuildExpression<Contact>(rule, entityMetadata);

			// Generated Expression: x => x.Company.EmployeeCount > 100
		}

		// ========================================================================
		// EXAMPLE 3: Collection Filtering with Any (ListEntity)
		// ========================================================================
		public void Example3_CollectionFilteringAny()
		{
			/*
			 * JSON Structure:
			 * {
			 *   "Logic": "AND",
			 *   "Criteria": [
			 *     {
			 *       "Data": "Addresses",
			 *       "Condition": "any",
			 *       "SubField": {
			 *         "Data": "Type",
			 *         "Condition": "=",
			 *         "Value": ["1"],
			 *         "FieldType": "select"
			 *       },
			 *       "FieldType": "listentity"
			 *     }
			 *   ]
			 * }
			 */

			var rule = new RolesNode
			{
				Logic = "AND",
				Criteria = new List<RolesNode>
				{
					new RolesNode
					{
						Data = "Addresses",
						Condition = "any",
						SubField = new RolesNode
						{
							Data = "Type",
							Condition = "=",
							Value = new List<string> { "1" },
							FieldType = "select"
						},
						FieldType = "listentity"
					}
				}
			};

			var entityMetadata = CreateContactMetadata();
			var expression = DynamicExpressionBuilder.BuildExpression<Contact>(rule, entityMetadata);

			// Generated Expression: x => x.Addresses.Any(item => item.Type == 1)
		}

		// ========================================================================
		// EXAMPLE 4: Persian DateTime Comparison
		// ========================================================================
		public void Example4_PersianDateTimeComparison()
		{
			/*
			 * JSON Structure:
			 * {
			 *   "Logic": "AND",
			 *   "Criteria": [
			 *     {
			 *       "Data": "CreatedOnShamsiDateTime",
			 *       "Condition": "<",
			 *       "Value": ["1404/10/09 10:40:16"],
			 *       "FieldType": "datetimeshamsi"
			 *     }
			 *   ]
			 * }
			 */

			var rule = new RolesNode
			{
				Logic = "AND",
				Criteria = new List<RolesNode>
				{
					new RolesNode
					{
						Data = "CreatedOn",
						Condition = "<",
						Value = new List<string> { "1404/10/09 10:40:16" },
						FieldType = "datetimeshamsi"
					}
				}
			};

			var entityMetadata = CreateContactMetadata();
			var expression = DynamicExpressionBuilder.BuildExpression<Contact>(rule, entityMetadata);

			// The Persian date "1404/10/09 10:40:16" is automatically converted to Gregorian DateTime
			// Generated Expression: x => x.CreatedOn < DateTime(converted_value)
		}

		// ========================================================================
		// EXAMPLE 5: Complex AND/OR Logic Groups
		// ========================================================================
		public void Example5_ComplexLogicGroups()
		{
			/*
			 * JSON Structure:
			 * {
			 *   "Logic": "AND",
			 *   "Criteria": [
			 *     {
			 *       "Data": "NationalCode",
			 *       "Condition": "contains",
			 *       "Value": ["1100"],
			 *       "FieldType": "string"
			 *     },
			 *     {
			 *       "Logic": "OR",
			 *       "Criteria": [
			 *         {
			 *           "Data": "Status",
			 *           "Condition": "=",
			 *           "Value": ["1"],
			 *           "FieldType": "select"
			 *         },
			 *         {
			 *           "Data": "Status",
			 *           "Condition": "=",
			 *           "Value": ["2"],
			 *           "FieldType": "select"
			 *         }
			 *       ]
			 *     }
			 *   ]
			 * }
			 */

			var rule = new RolesNode
			{
				Logic = "AND",
				Criteria = new List<RolesNode>
				{
					new RolesNode
					{
						Data = "NationalCode",
						Condition = "contains",
						Value = new List<string> { "1100" },
						FieldType = "string"
					},
					new RolesNode
					{
						Logic = "OR",
						Criteria = new List<RolesNode>
						{
							new RolesNode
							{
								Data = "Status",
								Condition = "=",
								Value = new List<string> { "1" },
								FieldType = "select"
							},
							new RolesNode
							{
								Data = "Status",
								Condition = "=",
								Value = new List<string> { "2" },
								FieldType = "select"
							}
						}
					}
				}
			};

			var entityMetadata = CreateContactMetadata();
			var expression = DynamicExpressionBuilder.BuildExpression<Contact>(rule, entityMetadata);

			// Generated Expression: 
			// x => (x.NationalCode != null && x.NationalCode.Contains("1100")) 
			//      && (x.Status == 1 || x.Status == 2)
		}

		// ========================================================================
		// EXAMPLE 6: Between Operator for Date Ranges
		// ========================================================================
		public void Example6_BetweenOperator()
		{
			/*
			 * JSON Structure:
			 * {
			 *   "Logic": "AND",
			 *   "Criteria": [
			 *     {
			 *       "Data": "CreatedOn",
			 *       "Condition": "between",
			 *       "Value": ["2024/01/01 00:00:00", "2024/12/31 23:59:59"],
			 *       "FieldType": "datetimeshamsi"
			 *     }
			 *   ]
			 * }
			 */

			var rule = new RolesNode
			{
				Logic = "AND",
				Criteria = new List<RolesNode>
				{
					new RolesNode
					{
						Data = "CreatedOn",
						Condition = "between",
						Value = new List<string> { "1403/01/01 00:00:00", "1403/12/29 23:59:59" },
						FieldType = "datetimeshamsi"
					}
				}
			};

			var entityMetadata = CreateContactMetadata();
			var expression = DynamicExpressionBuilder.BuildExpression<Contact>(rule, entityMetadata);

			// Generated Expression: 
			// x => x.CreatedOn >= DateTime(start) && x.CreatedOn <= DateTime(end)
		}

		// ========================================================================
		// EXAMPLE 7: Null/Not Null Checks
		// ========================================================================
		public void Example7_NullChecks()
		{
			/*
			 * JSON Structure:
			 * {
			 *   "Logic": "AND",
			 *   "Criteria": [
			 *     {
			 *       "Data": "ModifiedDate",
			 *       "Condition": "!null",
			 *       "FieldType": "datetime"
			 *     }
			 *   ]
			 * }
			 */

			var rule = new RolesNode
			{
				Logic = "AND",
				Criteria = new List<RolesNode>
				{
					new RolesNode
					{
						Data = "ModifiedDate",
						Condition = "!null",
						FieldType = "datetime"
					}
				}
			};

			var entityMetadata = CreateContactMetadata();
			var expression = DynamicExpressionBuilder.BuildExpression<Contact>(rule, entityMetadata);

			// Generated Expression: x => x.ModifiedDate != null
		}

		// ========================================================================
		// EXAMPLE 8: Boolean Field Conditions
		// ========================================================================
		public void Example8_BooleanConditions()
		{
			/*
			 * JSON Structure:
			 * {
			 *   "Logic": "AND",
			 *   "Criteria": [
			 *     {
			 *       "Data": "IsActive",
			 *       "Condition": "=",
			 *       "Value": ["true"],
			 *       "FieldType": "boolean"
			 *     }
			 *   ]
			 * }
			 */

			var rule = new RolesNode
			{
				Logic = "AND",
				Criteria = new List<RolesNode>
				{
					new RolesNode
					{
						Data = "IsActive",
						Condition = "=",
						Value = new List<string> { "true" },
						FieldType = "boolean"
					}
				}
			};

			var entityMetadata = CreateContactMetadata();
			var expression = DynamicExpressionBuilder.BuildExpression<Contact>(rule, entityMetadata);

			// Generated Expression: x => x.IsActive == true
		}

		// ========================================================================
		// EXAMPLE 9: Deep Nested Navigation (3 levels)
		// ========================================================================
		public void Example9_DeepNestedNavigation()
		{
			/*
			 * Query: Find contacts whose addresses are in a specific city
			 * 
			 * JSON Structure:
			 * {
			 *   "Logic": "AND",
			 *   "Criteria": [
			 *     {
			 *       "Data": "Addresses",
			 *       "Condition": "any",
			 *       "SubField": {
			 *         "Data": "City",
			 *         "Condition": "=",
			 *         "SubField": {
			 *           "Data": "Name",
			 *           "Condition": "=",
			 *           "Value": ["Tehran"],
			 *           "FieldType": "string"
			 *         },
			 *         "FieldType": "entity"
			 *       },
			 *       "FieldType": "listentity"
			 *     }
			 *   ]
			 * }
			 */

			var rule = new RolesNode
			{
				Logic = "AND",
				Criteria = new List<RolesNode>
				{
					new RolesNode
					{
						Data = "Addresses",
						Condition = "any",
						SubField = new RolesNode
						{
							Data = "City",
							Condition = "=",
							SubField = new RolesNode
							{
								Data = "Name",
								Condition = "=",
								Value = new List<string> { "Tehran" },
								FieldType = "string"
							},
							FieldType = "entity"
						},
						FieldType = "listentity"
					}
				}
			};

			var entityMetadata = CreateContactMetadata();
			var expression = DynamicExpressionBuilder.BuildExpression<Contact>(rule, entityMetadata);

			// Generated Expression: 
			// x => x.Addresses.Any(item => item.City.Name == "Tehran")
		}

		// ========================================================================
		// EXAMPLE 10: Complete Real-World Scenario
		// ========================================================================
		public void Example10_CompleteRealWorldScenario()
		{
			/*
			 * Business Rule: Notify users when a contact is created that meets ALL criteria:
			 * - NationalCode contains "1100"
			 * - Has at least one address of Type 1
			 * - Created before a specific Persian date OR modified after another date
			 * - Is currently active
			 * 
			 * This is the example from the original request
			 */

			string jsonString = @"
			{
			  ""Logic"": ""AND"",
			  ""Criteria"": [
			    {
			      ""Data"": ""NationalCode"",
			      ""Condition"": ""contains"",
			      ""Value"": [""1100""],
			      ""FieldType"": ""string""
			    },
			    {
			      ""Data"": ""Addresses"",
			      ""Condition"": ""any"",
			      ""SubField"": {
			        ""Data"": ""Type"",
			        ""Condition"": ""="",
			        ""Value"": [""1""],
			        ""FieldType"": ""select""
			      },
			      ""FieldType"": ""listentity""
			    },
			    {
			      ""Logic"": ""OR"",
			      ""Criteria"": [
			        {
			          ""Data"": ""CreatedOn"",
			          ""Condition"": ""<"",
			          ""Value"": [""1404/10/09 10:40:16""],
			          ""FieldType"": ""datetimeshamsi""
			        },
			        {
			          ""Data"": ""ModifiedDate"",
			          ""Condition"": "">"",
			          ""Value"": [""1404/07/15 10:40:02""],
			          ""FieldType"": ""datetimeshamsi""
			        }
			      ]
			    },
			    {
			      ""Data"": ""IsActive"",
			      ""Condition"": ""="",
			      ""Value"": [""true""],
			      ""FieldType"": ""boolean""
			    }
			  ]
			}";

			var rule = JsonConvert.DeserializeObject<RolesNode>(jsonString);
			var entityMetadata = CreateContactMetadata();
			var expression = DynamicExpressionBuilder.BuildExpression<Contact>(rule, entityMetadata);

			// Usage with EF Core:
			// var affectedContacts = context.Contacts.Where(expression).ToList();

			// Generated Expression (simplified):
			// x => (x.NationalCode != null && x.NationalCode.Contains("1100"))
			//      && x.Addresses.Any(item => item.Type == 1)
			//      && (x.CreatedOn < DateTime(...) || x.ModifiedDate > DateTime(...))
			//      && x.IsActive == true
		}

		// ========================================================================
		// Helper: Create Sample EntityMetadata
		// ========================================================================
		private EntityMetadata CreateContactMetadata()
		{
			return new EntityMetadata
			{
				EntityName = "Contact",
				EntityFullName = "YourNamespace.Contact",
				Properties = new List<PropertyMetadata>
				{
					new PropertyMetadata
					{
						Name = "NationalCode",
						DisplayName = "National Code",
						SystemType = Common.Attributes.SystemType.String,
						SearchPath = "NationalCode"
					},
					new PropertyMetadata
					{
						Name = "FirstName",
						DisplayName = "First Name",
						SystemType = Common.Attributes.SystemType.String,
						SearchPath = "FirstName"
					},
					new PropertyMetadata
					{
						Name = "CreatedOn",
						DisplayName = "Created On",
						SystemType = Common.Attributes.SystemType.DateTime,
						SearchPath = "CreatedOn"
					},
					new PropertyMetadata
					{
						Name = "ModifiedDate",
						DisplayName = "Modified Date",
						SystemType = Common.Attributes.SystemType.DateTime,
						SearchPath = "ModifiedDate"
					},
					new PropertyMetadata
					{
						Name = "Status",
						DisplayName = "Status",
						SystemType = Common.Attributes.SystemType.Select,
						SearchPath = "Status"
					},
					new PropertyMetadata
					{
						Name = "IsActive",
						DisplayName = "Is Active",
						SystemType = Common.Attributes.SystemType.Boolean,
						SearchPath = "IsActive"
					},
					new PropertyMetadata
					{
						Name = "Addresses",
						DisplayName = "Addresses",
						SystemType = Common.Attributes.SystemType.ListEntity,
						SearchPath = "Addresses",
						RelatedEntityTypeFullName = "YourNamespace.Address"
					},
					new PropertyMetadata
					{
						Name = "Company",
						DisplayName = "Company",
						SystemType = Common.Attributes.SystemType.Entity,
						SearchPath = "Company",
						RelatedEntityTypeFullName = "YourNamespace.Company"
					}
				}
			};
		}
	}
}

#endif

