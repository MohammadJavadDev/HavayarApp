using Common.Attributes;
using Common.Auth.Enums;
using Data;
using Data.Contracts;
using Data.SystemAuth;
using Entities.App.SLS;
using Entities.Base.DataTable;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.Dynamic
{
	[Route("Panel/[controller]")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("اعلامیه قیمت راهکاران")]
	public class PriceListController(RahkaranDbContext dbContext) : BaseController
	{
		  
		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List()
		{
			return View(@"\Views\Panel\SLS\PriceList\List.cshtml");
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ایجاد اعلامیه قیمت", ActionAccessType.Api, ActionAccessItemType.Custom)]
		public async Task<IActionResult> CreatePartList()
		{

			var query = @"
						declare @PriceListLastId bigint = 0;
						declare @PriceListItemLastId bigint = 0;
						declare @CountListItems bigint = (select count(0)
												    from PriceListCode); -- حتما تعداد کد ها باید اینجا گذاشته بشه
						DECLARE @StartDate DATETIME = CAST(GETDATE() AS DATE);
						DECLARE @EndDate   DATETIME = DATEADD(DAY, 6, @StartDate);
						set @EndDate = DATEADD(minute ,-1 , @EndDate)

 
						declare @PriceListResultRowId bigint =0;

						exec sys3.spGetNextId @TableName = 'SLS3.PriceList', @Id = null, @IncValue = 1, @IsLegacy = 0

						exec sys3.spGetNextId @TableName = 'SLS3.PriceListItem', @Id = null, @IncValue = 1, @IsLegacy = 0


						set @PriceListLastId = (Select LastId from Sys3.TableIdGen
						where TableName ='SLS3.PriceList')


						 set @PriceListItemLastId = (Select LastId from Sys3.TableIdGen
						where TableName ='SLS3.PriceListItem')

						exec sys3.spGetNextId @TableName = 'SLS3.PriceListItem', @Id = null, @IncValue = @CountListItems , @IsLegacy = 0


						insert into SLS3.PriceList (PriceListID, PriceListParametersRef, Number, Title, StartDate, EndDate, PriceListType, SalesDocumentType, CurrencyRef, State, CreationDate, LastModificationDate, Creator, LastModifier)
						values  (@PriceListLastId, 10, @PriceListLastId, N'اعلامیه قیمت ثبت خودکار توسط سیستم', @StartDate, @EndDate, 1, 2, 1, 1, GETDATE(), GETDATE(), 1, 1);


						with Prices as (
						SELECT
						    iif(InternalNetSalesPrice <  ExternalNetSalesPrice
							   ,isnull(ExternalNetSalesPrice,InternalNetSalesPrice) ,
							   isnull(InternalNetSalesPrice,ExternalNetSalesPrice) ) FinalPrice,
							   row_number() over (order by t.PartId) rn,
							    pp.ProductID,
							    pp.UnitRef
						FROM dbo.PriceListCode t
						inner join dbo.Vw_Sales_PartPrice vspp on t.PartId = vspp.PartID
						inner join SLS3.Product pp on t.PartId = pp.PartRef and pp.State = 1
						and (InternalSalesPrice is not null  or ExternalSalesPrice is not null)
						)


						insert into SLS3.PriceListItem (PriceListItemID, PriceListRef, RowNumber, ProductRef, ProductGroupRef, ProductUnitRef, PackageRef, Quantity, CurrencyRef, Fee, Definitiveness, ReductionTolerance, AdditionTolerance)
						select    (p.rn+@PriceListItemLastId) ,@PriceListLastId, p.rn, p.ProductID, null, p.UnitRef, null, null, 1, p.FinalPrice, 2, 0.000000, 100.000000
						from Prices p
						where p.FinalPrice <> 0

 
						exec sys3.spGetNextId @TableName = 'SLS3.PriceListResultRow', @Id = null, @IncValue = 1, @IsLegacy = 0

						set @PriceListResultRowId = (Select LastId from Sys3.TableIdGen
						where TableName ='SLS3.PriceListResultRow')

						insert into SLS3.PriceListResultRow (PriceListResultRowID, PriceListRef,SalesTypeRef ,SalesOfficeRef)
						values  (@PriceListResultRowId+2, @PriceListLastId,  13 ,12),
							   (@PriceListResultRowId+3, @PriceListLastId,  10 ,12),
							   (@PriceListResultRowId+5, @PriceListLastId,  3 ,12),
							   (@PriceListResultRowId+6, @PriceListLastId,  13 ,13),
							   (@PriceListResultRowId+7, @PriceListLastId,  10 ,13),
							   (@PriceListResultRowId+8, @PriceListLastId,  3 ,13);

						 exec sys3.spGetNextId @TableName = 'SLS3.PriceListResultRow', @Id = null, @IncValue = 6, @IsLegacy = 0

						";
			 await dbContext.Database.ExecuteSqlRawAsync(query);

			 

			return Ok();
		}

	}
}
