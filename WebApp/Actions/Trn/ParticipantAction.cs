using System.Text;
using Data.Contracts;
using Data.Contracts.Actions;
using Entities.App.Trn;
using Microsoft.EntityFrameworkCore;

namespace WebApp.Actions.Trn
{
	/// <summary>
	/// کد ملی شرکت‌کننده یکتا است. خالی مجاز است و چند رکورد بدون کد ملی می‌توانند بمانند.
	/// </summary>
	public class ParticipantAction(IUnitOfWork unitOfWork)
	{
		[EntityAction(typeof(Participant), EntityActionTrigger.BeforeSave, "RejectDuplicateNationalCode", "جلوگیری از کد ملی تکراری", Priority = 1)]
		public async Task RejectDuplicateNationalCode(Participant entity, CancellationToken ct)
		{
			var code = NormalizeNationalCode(entity.NationalCode);
			entity.NationalCode = code;
			if (code == null)
				return;

			var currentId = entity.Id ?? 0;
			var lookupCodes = LookupCodes(code);
			var matches = await unitOfWork.Repository<Participant>().TableNoTracking
				.Where(p => p.Id != currentId
					&& p.NationalCode != null
					&& lookupCodes.Contains(p.NationalCode.Replace(" ", "").Trim()))
				.OrderBy(p => p.Id)
				.Select(p => new { p.FirstName, p.LastName })
				.Take(3)
				.ToListAsync(ct);

			if (matches.Count == 0)
				return;

			var names = string.Join("، ", matches
				.Select(m => $"{m.FirstName} {m.LastName}".Trim())
				.Where(n => n.Length > 0));

			var message = string.IsNullOrEmpty(names)
				? $"کد ملی «{code}» قبلاً ثبت شده است."
				: $"کد ملی «{code}» قبلاً برای {names} ثبت شده است.";
			throw new InvalidOperationException(message);
		}

		private static string? NormalizeNationalCode(string? value)
		{
			if (string.IsNullOrWhiteSpace(value))
				return null;

			var compact = new StringBuilder(value.Length);
			foreach (var ch in value.Trim())
			{
				if (ch is ' ' or '\t' or '\u00A0' or '\u200c' or '-')
					continue;
				compact.Append(ToAsciiDigit(ch));
			}

			if (compact.Length == 0)
				return null;

			var code = compact.ToString();
			return code.All(char.IsDigit) ? code : value.Trim();
		}

		private static List<string> LookupCodes(string code)
		{
			var codes = new List<string> { code.Replace(" ", "") };
			if (!code.All(char.IsDigit))
				return codes;

			codes.Add(MapDigits(code, "۰۱۲۳۴۵۶۷۸۹"));
			codes.Add(MapDigits(code, "٠١٢٣٤٥٦٧٨٩"));
			return codes;
		}

		private static string MapDigits(string code, string digits)
		{
			var mapped = new char[code.Length];
			for (var i = 0; i < code.Length; i++)
			{
				var ch = code[i];
				mapped[i] = ch is >= '0' and <= '9' ? digits[ch - '0'] : ch;
			}
			return new string(mapped);
		}

		private static char ToAsciiDigit(char ch) => ch switch
		{
			'۰' or '٠' => '0',
			'۱' or '١' => '1',
			'۲' or '٢' => '2',
			'۳' or '٣' => '3',
			'۴' or '٤' => '4',
			'۵' or '٥' => '5',
			'۶' or '٦' => '6',
			'۷' or '٧' => '7',
			'۸' or '٨' => '8',
			'۹' or '٩' => '9',
			_ => ch
		};
	}
}
