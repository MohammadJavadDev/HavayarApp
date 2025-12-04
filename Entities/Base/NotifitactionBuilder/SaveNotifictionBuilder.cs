using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Entities.Base.NotifitactionBuilder
{
	public class SaveNotificationBuidler
	{
 
		public string EntityFullName { get; set; } = string.Empty;

 
		public SaveNotificationBuidlerAction Create { get; set; } = new();

 
		public SaveNotificationBuidlerAction Edit { get; set; } = new();

 
		public SaveNotificationBuidlerAction Delete { get; set; } = new();
	}

	public class SaveNotificationBuidlerAction
	{
 
		public bool Enable { get; set; }

 
		public RolesNode Roles { get; set; } = new();

 
		public string Title { get; set; } = string.Empty;
	}

	/// <summary>
	/// Represents either a group node (logic + criteria) or a condition node (data + condition + subField + value + fieldType).
	/// SubField may be null, a simple string path, or a nested object (condition node) – handled via JToken.
	/// </summary>
 
	public class RolesNode
	{
		
		public string? Logic { get; set; }

 
		public List<RolesNode>? Criteria { get; set; }

 
		public string? Data { get; set; }

 
		public string? Condition { get; set; }

 
		public RolesNode? SubField { get; set; }

 
		public List<string>? Value { get; set; }

 
		public string? FieldType { get; set; }


	}
}
