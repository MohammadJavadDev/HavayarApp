using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.DataProtection;
namespace Services
{
	public class PayloadProtector
	{
		private readonly IDataProtector _protector;

		public PayloadProtector(IDataProtectionProvider provider)
		{
			// Create a protector with a unique purpose string
			_protector = provider.CreateProtector("EntitySelector.SecureParams.v1");
		}

		public string Protect(string plainText)
		{
			if (string.IsNullOrEmpty(plainText)) return null;
			return _protector.Protect(plainText);
		}

		public string Unprotect(string cipherText)
		{
			if (string.IsNullOrEmpty(cipherText)) return null;
			try
			{
				return _protector.Unprotect(cipherText);
			}
			catch (System.Security.Cryptography.CryptographicException)
			{
				// This happens if the user tampered with the string
				// or if the keys were rotated/expired.
				return null;
			}
		}
	}
}
