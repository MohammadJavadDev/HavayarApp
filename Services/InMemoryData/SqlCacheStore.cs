using Common.Entities;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Services.InMemoryData
{
	public class SqlCacheStore
	{
		private readonly IMemoryCache _cache;

		public SqlCacheStore(IMemoryCache cache)
		{
			_cache = cache;
		}

		public string GetOrSet(SelectorDefinition definition)
		{
		 
			string rawContent = JsonSerializer.Serialize(definition);
			string key = ComputeSha256Hash(rawContent);

			if (!_cache.TryGetValue("DEF_" + key, out _))
			{
				_cache.Set("DEF_" + key, definition, TimeSpan.FromHours(24));
			}
			return key;
		}

		public SelectorDefinition Get(string key)
		{
			_cache.TryGetValue("DEF_" + key, out SelectorDefinition def);
			return def;
		}

		private static string ComputeSha256Hash(string rawData)
		{
			using (SHA256 sha256Hash = SHA256.Create())
			{
				byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));
				StringBuilder builder = new StringBuilder();
				for (int i = 0; i < bytes.Length; i++) builder.Append(bytes[i].ToString("x2"));
				return builder.ToString();
			}
		}
	}
}
