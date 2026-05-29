using System;
using System.Collections.Generic;
using System.Text;

namespace Data.Contracts.Actions
{
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
	public class EntityActionAttribute : Attribute
	{
		public Type EntityType { get; }
		public EntityActionTrigger Trigger { get; }
		public string Name { get; }
		public string Title { get; }
		public int Priority { get; set; } = 100;
		public bool IsActive { get; set; } = true;

		public EntityActionAttribute(Type entityType, EntityActionTrigger trigger, string name, string title)
		{
			EntityType = entityType;
			Trigger = trigger;
			Name = name;
			Title = title;
		}
	}
	public enum EntityActionTrigger
	{
		BeforeAdd,
		AfterAdd,
		BeforeUpdate,
		AfterUpdate,
		BeforeDelete,
		AfterDelete,
		BeforeSave, // قبل از تشخیص اینکه عملیات Add است یا Update
        AfterSave
    }
}