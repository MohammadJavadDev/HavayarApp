using Entities.Auth;

namespace Services.AccessServices;

public interface IAccessMemoryStorage
{
     List<AccessController> GetAllAccessControllers();
	AccessController? GetAccessControllerBy(Type type);
	AccessController? GetAccessControllerBy(string fullName);
     void SetAccessControllers(List<AccessController> accessControllers);
     public bool ExistPath(string path);
     public AccessAction? GetAccessAction(string path);
	string? GetMatchingTemplate(string path);
	List<AccessController> GetAllAccessControllersRedis();
	AccessController? GetAccessControllerByRedis(Type type);
	AccessController? GetAccessControllerByRedis(string fullName);
	void SetAccessControllersRedis(List<AccessController> accessControllers);
	public bool ExistPathRedis(string path);
	public AccessAction? GetAccessActionRedis(string path);
}