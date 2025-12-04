using Common.Attributes;
using Common.Auth.Enums;
 

namespace Entities.Auth;

public class AccessController 
{
    public long Id { get; set; }  
    public string Name { get; set; }
    public string DisplayName { get; set; }
    public string Path { get; set; }
	public Type? EntityType { get; set; }
	public List<AccessAction> Actions { get; set; }
}

public class AccessAction  
{
    public long Id { get; set; }
    public string Name { get; set; }
    public string DisplayName { get; set; }
    public string Path { get; set; }
    public string HttpMethod { get; set; }
    public AccessController AccessController { get; set; }
    public long? AccessControllerId { get; set; }
    public ActionAccessType? ActionAccessType  { get; set; }
    public ActionAccessItemType ActionAccessItemType  { get; set; }
}