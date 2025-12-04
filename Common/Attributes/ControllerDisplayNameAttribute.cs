namespace Common.Attributes;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class ControllerInfoAttribute : Attribute
{
    public string Name { get; }  
	public Type? EntityType { get; }
	public ControllerInfoAttribute(string name)
	{
		this.Name = name;
	}

	public ControllerInfoAttribute(string name, Type entityType)
	{
		this.Name = name;
		this.EntityType = entityType;
	}
}