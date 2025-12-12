namespace Data.SystemAuth;

public interface ISdk
{
    CurrentUser CurrentUser { get; }
    public bool Authenticated { get;}
	void RefreshUserRole();

}