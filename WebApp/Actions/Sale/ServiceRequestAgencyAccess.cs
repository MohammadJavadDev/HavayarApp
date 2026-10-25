using System.Data;
using Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace WebApp.Actions.Sale;

/// <summary>
/// محدوده اندیکاتور برای نقش نمایندگی.
/// هم‌تراز با فیلتر نمایه AfterSales_ServiceRequest_Agency در Seed_SaleServiceRequest_AgencyProfile.sql.
/// </summary>
public static class ServiceRequestAgencyAccess
{
	public const string DeniedMessage = "این اندیکاتور در محدوده دسترسی نمایندگی شما نیست";

	public static async Task<bool> IsHiddenFromAgencyAsync(
		ApplicationDbContext db,
		bool isAgencyUser,
		long? userId,
		long serviceRequestId,
		CancellationToken cn)
	{
		if (!isAgencyUser)
			return false;
		if (userId is null or <= 0 || serviceRequestId <= 0)
			return true;
		return !await CanSeeAsync(db, userId.Value, serviceRequestId, cn);
	}

	public static async Task<bool> CanSeeAsync(
		ApplicationDbContext db,
		long userId,
		long serviceRequestId,
		CancellationToken cn)
	{
		var connection = db.Database.GetDbConnection();
		var openedHere = connection.State != ConnectionState.Open;
		if (openedHere)
			await connection.OpenAsync(cn);
		try
		{
			await using var command = connection.CreateCommand();
			command.CommandText = CanSeeSql;
			command.CommandTimeout = 60;
			var transaction = db.Database.CurrentTransaction;
			if (transaction != null)
				command.Transaction = transaction.GetDbTransaction();

			var userParameter = command.CreateParameter();
			userParameter.ParameterName = "@CuId";
			userParameter.Value = userId;
			command.Parameters.Add(userParameter);

			var requestParameter = command.CreateParameter();
			requestParameter.ParameterName = "@RequestId";
			requestParameter.Value = serviceRequestId;
			command.Parameters.Add(requestParameter);

			var scalar = await command.ExecuteScalarAsync(cn);
			return scalar switch
			{
				int value => value == 1,
				long value => value == 1,
				_ => false
			};
		}
		finally
		{
			if (openedHere)
				await connection.CloseAsync();
		}
	}

	private const string CanSeeSql = """
		DECLARE @Allowed TABLE (UserId BIGINT NOT NULL PRIMARY KEY);
		DECLARE @Managers TABLE (UserId BIGINT NOT NULL PRIMARY KEY);
		DECLARE @Provinces TABLE (ProvinceId BIGINT NOT NULL PRIMARY KEY);
		DECLARE @ZoneId BIGINT;

		INSERT INTO @Allowed (UserId)
		SELECT DISTINCT x.UserId
		FROM (
		    SELECT u.Id AS UserId
		    FROM [system].[User] AS me
		    INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] AS huMe
		        ON LOWER(LTRIM(RTRIM(huMe.Username))) = LOWER(LTRIM(RTRIM(me.Username)))
		    INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] AS hu
		        ON hu.Company_FK = huMe.Company_FK
		    INNER JOIN [system].[User] AS u
		        ON LOWER(LTRIM(RTRIM(u.Username))) = LOWER(LTRIM(RTRIM(hu.Username)))
		    WHERE me.Id = @CuId AND huMe.Company_FK IS NOT NULL
		    UNION
		    SELECT @CuId WHERE @CuId IS NOT NULL
		) AS x
		WHERE x.UserId IS NOT NULL;

		INSERT INTO @Provinces (ProvinceId)
		SELECT DISTINCT r.Id
		FROM [system].[User] AS me
		INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] AS hu
		    ON LOWER(LTRIM(RTRIM(hu.Username))) = LOWER(LTRIM(RTRIM(me.Username)))
		INNER JOIN [TMS].[TotalSystem].[dbo].[Crm_Customer] AS c
		    ON c.ManCompany_FK = hu.Company_FK
		INNER JOIN [TMS].[TotalSystem].[dbo].[Crm_Customer_Address] AS a
		    ON a.Customer_FK = c.Customer_ID
		INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_Province] AS pr
		    ON pr.Province_ID = a.Province_FK
		INNER JOIN [Gnr].[Region] AS r
		    ON r.Type = 2
		   AND (
		        r.Name = pr.Province_Title
		        OR r.Name = N'استان ' + pr.Province_Title
		        OR REPLACE(r.Name, N'استان ', N'') = pr.Province_Title
		   )
		WHERE me.Id = @CuId
		  AND hu.Company_FK IS NOT NULL
		  AND NULLIF(LTRIM(RTRIM(pr.Province_Title)), N'') IS NOT NULL;

		SELECT TOP (1) @ZoneId = s.ZoneId
		FROM (
		    SELECT z.Id AS ZoneId,
		           COUNT(DISTINCT ap.ProvinceId) AS Hit,
		           COUNT(DISTINCT rz2.ProvinceId) AS Cnt
		    FROM [Crm].[Zone] AS z
		    INNER JOIN [Sale].[ResponsibleZone] AS rz2 ON rz2.ZoneId = z.Id
		    LEFT JOIN @Provinces AS ap ON ap.ProvinceId = rz2.ProvinceId
		    WHERE z.Title LIKE N'منطقه [0-9]%'
		      AND z.Title NOT LIKE N'%-%'
		      AND z.Title NOT LIKE N'%نمایندگی%'
		    GROUP BY z.Id
		) AS s
		WHERE s.Hit > 0
		ORDER BY s.Hit DESC, s.Cnt ASC;

		INSERT INTO @Managers (UserId)
		SELECT DISTINCT rz.ResponsibleId
		FROM [Sale].[ResponsibleZone] AS rz
		WHERE rz.ZoneId = @ZoneId AND rz.ResponsibleId IS NOT NULL;

		SELECT CASE WHEN EXISTS (
		    SELECT 1
		    FROM [Sale].[ServiceRequest] AS t1
		    LEFT JOIN [SLS].[CustomerAddress] AS t4 ON t4.Id = t1.CustomerAddressId
		    WHERE t1.Id = @RequestId
		      AND (
		            t1.CreatedById IN (SELECT UserId FROM @Allowed)
		            OR (
		                t1.CreatedById IN (SELECT UserId FROM @Managers)
		                AND t4.ProvinceId IN (SELECT ProvinceId FROM @Provinces)
		            )
		      )
		) THEN 1 ELSE 0 END;
		""";
}
