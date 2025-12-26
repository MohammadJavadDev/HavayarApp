using System;
using System.ComponentModel;
using System.Net;
using System.Runtime.InteropServices;

namespace App.BackgroundJob.Models
{

	public class NetworkConnection : IDisposable
	{
		private readonly string _networkName;

		public NetworkConnection(string networkName, NetworkCredential credentials)
		{
			_networkName = networkName;

			// قطع اتصالات قبلی به همان Share (برای جلوگیری از 1219)
			WNetCancelConnection2(networkName, 0, true);

			var netResource = new NetResource
			{
				Scope = ResourceScope.GlobalNetwork,
				ResourceType = ResourceType.Disk,
				DisplayType = ResourceDisplaytype.Share,
				RemoteName = networkName
			};

			var result = WNetAddConnection2(netResource, credentials.Password, credentials.UserName, 0);

			if (result != 0)
				throw new Win32Exception(result, $"خطا در اتصال به مسیر شبکه: {result}");
		}

		public void Dispose()
		{
			WNetCancelConnection2(_networkName, 0, true);
		}

		[DllImport("mpr.dll")]
		private static extern int WNetAddConnection2(NetResource netResource, string password, string username, int flags);

		[DllImport("mpr.dll")]
		private static extern int WNetCancelConnection2(string name, int flags, bool force);
		public class NetResource
		{
			public ResourceScope Scope;
			public ResourceType ResourceType;
			public ResourceDisplaytype DisplayType;
			public string RemoteName;
		}

		public enum ResourceScope { GlobalNetwork }
		public enum ResourceType { Disk }
		public enum ResourceDisplaytype { Share }

	}



	public class FileServerHelper()
	{
		public byte[] GetFileFromNetwork(string filePath)
		{
			var networkPath = @"\\172.20.40.27\Uploads";

			var credential = new NetworkCredential(
			    "mirlohi.m@havayar.com",          // مثلا: fileserverUser
			    "Mj@09876",          // پسورد
			   null            // اگر دامین دارد، در غیر اینصورت خالی
			);

			using (new NetworkConnection(networkPath, credential))
			{
				if (!File.Exists(filePath))
					throw new FileNotFoundException("فایل پیدا نشد");

				return File.ReadAllBytes(filePath);
			}
		}
	}

	public class NetResource
	{
		public ResourceScope Scope;
		public ResourceType ResourceType;
		public ResourceDisplaytype DisplayType;
		public string RemoteName;
	}

	public enum ResourceScope { GlobalNetwork }
	public enum ResourceType { Disk }
	public enum ResourceDisplaytype { Share }



}
