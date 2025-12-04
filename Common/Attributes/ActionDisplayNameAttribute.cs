using Common.Auth.Enums;
 

namespace Common.Attributes;

[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public class ActionDisplayNameAttribute : Attribute
{
	public string Name { get; } 
	public ActionAccessType Type { get; } 
	public ActionAccessItemType ActionAccessItemType { get; } 
	public ActionDisplayNameAttribute(string name, ActionAccessType type, ActionAccessItemType ActionAccessItemType)
	{
	this.Name  = name;
		this.Type  = type;
		this.ActionAccessItemType  = ActionAccessItemType;
}
	public ActionDisplayNameAttribute(string name, ActionAccessType type)
	{
		this.Name = name;
		this.Type = type;
		
	}


}

