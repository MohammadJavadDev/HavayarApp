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
}