namespace Data.SystemAuth;

public interface ISdk
{
	CurrentUser CurrentUser { get; }
	public bool Authenticated { get; }
	void RefreshUserRole();

	#region Role Checking Methods
	/// <summary>
	/// بررسی داشتن نقش بر اساس نام نقش
	/// </summary>
	bool HasRole(string roleName);

	/// <summary>
	/// بررسی داشتن نقش بر اساس شناسه نقش
	/// </summary>
	bool HasRole(long roleId);

	/// <summary>
	/// بررسی داشتن حداقل یکی از نقش‌های مشخص شده
	/// </summary>
	bool HasAnyRole(params string[] roleNames);

	/// <summary>
	/// بررسی داشتن تمامی نقش‌های مشخص شده
	/// </summary>
	bool HasAllRoles(params string[] roleNames);

	/// <summary>
	/// بررسی اینکه آیا کاربر ادمین است یا خیر
	/// </summary>
	bool IsAdministrator { get; }
	#endregion
}