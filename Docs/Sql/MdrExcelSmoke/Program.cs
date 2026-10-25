using Data.Services.Edms;

var licensePath = @"d:\Projects\Havayar\HavayarApp\WebApp\wwwroot\Aspose.Total.NET.lic";
var report = new MdrReportResponse
{
	Items = new List<MdrReportItemDto>(1)
	{
		new MdrReportItemDto
		{
			ProjectName = "Air  compressor package bandar abbas phosphoric Acid Abad rahan Pars",
			DocumentNumber = "81A1-UT-CD-000-GN-LST-004",
			DocumentTitle = "Vendor Print Index & Schedule (VPIS) of Air System",
			BeneficiaryUnit = "فروش تجهیزات فشرده سازی و جداسازی هوا و گاز",
			Disipline = "GN",
			Responsible = "MirMohsen Saeedi",
			Checker = "MirMohsen Saeedi",
			Approver = " Khalaj Mohammad",
			DocClass = "IFA",
			DocType = "LST",
			ConsumedManHoursInText = "10:00",
			FirstIssueDate = "2025-08-13",
			HoldDocuments = false,
			LastRev = "1",
			FinalStatus = "Not Review",
			InternalStatus = "Issued",
			ClientRepliedTime = "52",
			HavayarLastSendDate = "2026-05-09",
			HavayarLastSendTransmittal = "reply-81A1-UT-CD-000-GN-LST-004-01",
			HavayarRdsDate = "2026-05-06",
			HavayarRdsNo = "reply-81A1-UT-CD-000-GN-LST-004-01",
			LastPoi = "IFA",
			ClientLastSentDate = "2026-02-07",
			Revisions = new List<MdrRevisionDto>(2)
			{
				new MdrRevisionDto
				{
					Index = 0,
					RevNo = "0",
					HavayarRdsDate = "1404/10/15",
					Poi = "IFA",
					ClientReplyDate = "2026-02-03",
					Status = "ReIssued"
				},
				new MdrRevisionDto
				{
					Index = 1,
					RevNo = "1",
					HavayarSendingDate = "2026-02-03",
					HavayarSendingTransmittal = "GBA006-HVY-ARP-TRS--0091",
					HavayarRdsDate = "1405/02/16",
					HavayarRdsNo = "reply-81A1-UT-CD-000-GN-LST-004-01",
					Poi = "IFA",
					ClientTime = "3",
					ClientReplyDate = "2026-02-07",
					HavayarDelay = "0",
					ClientDelay = "0",
					Status = "Not Review"
				}
			}
		}
	}
};

var outPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "mdr_export_test.xlsx"));
await using (var fs = File.Create(outPath))
{
	EdmsMdrReportExcelExporter.Export(report, fs, Path.GetFullPath(licensePath));
}

Console.WriteLine("Wrote: " + outPath);
