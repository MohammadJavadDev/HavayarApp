using Entities.Auth;

namespace Services.AccessServices;

public class AccessMemoryStorage : IAccessMemoryStorage
{
 
    private List<AccessController> _accessControllers;
    private List<AccessAction> _accessAction;
    private List<string> _accessPaths;

    public AccessMemoryStorage()
    {
 
        _accessControllers = new List<AccessController>();
        _accessPaths = new List<string>();
        _accessAction = new List<AccessAction>();
    }

 
    public List<AccessController> GetAllAccessControllers()
    {
        return _accessControllers;
    }
 
    public void SetAccessControllers(List<AccessController> accessControllers)
    {
        _accessControllers = accessControllers;
        _accessAction = accessControllers.SelectMany(c => c.Actions).ToList();
        _accessPaths = accessControllers.SelectMany(c => c.Actions).ToList()
            .Select(c=>c.Path).ToList();
    }

    public bool ExistPath(string path)
    {
        
        return _accessPaths.Any(c=>c == path);
    }

    public AccessAction? GetAccessAction(string path)
    {
  
        return _accessAction.FirstOrDefault(x => x.Path.ToLower().Equals(path.ToLower()));
    }

	AccessController? IAccessMemoryStorage.GetAccessControllerBy(Type type)
	{
          return _accessControllers.FirstOrDefault(x => x.EntityType == type);
	}

	AccessController? IAccessMemoryStorage.GetAccessControllerBy(string fullName)
	{
		return _accessControllers.FirstOrDefault(x => x.EntityType.FullName == fullName);
	}
}