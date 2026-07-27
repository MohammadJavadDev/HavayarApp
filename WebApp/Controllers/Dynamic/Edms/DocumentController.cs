using Common.Attributes;
using Common.Auth.Enums;
using Common.Utilities;
using Data;
using Data.Contracts;
using Data.SystemAuth;
using Entities.App.Bom;
using Entities.App.Edms;
using Entities.App.Edms.Enums;
using Entities.App.Gnr;
using Entities.App.Inv;
using Entities.App.Sup;
using Entities.App.Sup.Enums;
using Entities.Auth;
using Entities.Base;
using Entities.Base.DataTable;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Services.Auth;
using Data.Services.Edms;
using WebApp.ViewModels.Edms;
using WebFramework.Filtters;
using WebFramework.Page;
 

namespace WebApp.Controllers.Dynamic
{
	[Route("Panel/Edms/[controller]")]
	[ApiController]
    [ApiResultFilter]
    [ControllerInfo("مدارک مهندسی", typeof(Document))]
    public class DocumentController(
		IUnitOfWork unitOfWork,
          IWebHostEnvironment _webHostEnvironment,
		ApplicationDbContext db,
		IEdmsMdrReportService mdrReportService) : BaseController
    {
        [HttpPost("[action]/{CanEditDocument?}")]
        [ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
        public async Task<IActionResult> Save(Document document,bool? CanEditDocument , CancellationToken cn)
        {

			// پیدا کردن آخرین ریویژن موجود برای این سند
			var maxRevision = await unitOfWork.Repository<Document>()
			    .TableNoTracking
			    .Where(d => d.ProjectId == document.ProjectId.Value
						 && d.DocumentVpisId == document.DocumentVpisId.Value)
			    .MaxAsync(c => c.Revision);

			// اگر Document جدید است و ریویژن 1 دارد یا ریویژن جدیدتر از آخرین است
			if (maxRevision == null || document.Revision > maxRevision)
			{
			 
				await unitOfWork.Repository<Document>()
				    .BulkUpdateFieldAsync(
					   d => d.ProjectId == document.ProjectId.Value
						   && d.DocumentVpisId == document.DocumentVpisId.Value,
						   c => c.IsLatest,
						   false,
						   cn);  

				// ریویژن جدید را true کن
				document.IsLatest = true;
			}
			else if(maxRevision != document.Revision)
			{
				// اگر ریویژن جدیدتر از آخرین نبود، false باشد
				document.IsLatest = false;
			}
			else if (maxRevision == document.Revision)
			{
				document.IsLatest = true;
			}

				// Save logic here
		 if (document.Id == null || document.Id == 0)
            {
                return await Add(document, CanEditDocument, cn);
            }
            var exist = await unitOfWork.Repository<Document>().TableNoTracking.AnyAsync(c => c.Id == document.Id);
            if (exist)
            {
                return await Update(document, CanEditDocument, cn);
            }
            return await Add(document, CanEditDocument, cn);
        }

        [HttpPost("[action]")]
        [ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
        public async Task<IActionResult> Add(Document document, bool? CanEditDocument, CancellationToken cn)
        {

               if(!(CanEditDocument ?? false))
               {
                    document.Comments = null;

			}
               document.Status = DocumentStatusEnums.Issue;
			 
			// Add logic here
			var entity = await unitOfWork.Repository<Document>().AddAsync(document, cn, true);

			var newDocumentReplay = new DocumentComment()
			{
				DocumentId = entity.Id.Value,
				Comment = "Issue",
				Status = DocumentStatusEnums.Issue
			};

			await unitOfWork.Repository<DocumentComment>().AddAsync(newDocumentReplay, cn, false, false,false);
			return Ok(entity);
        }

        [HttpPost("[action]")]
        [ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
        public async Task<IActionResult> Update(Document document, bool? CanEditDocument, CancellationToken cn)
        {
			 
			if (!(CanEditDocument ?? false))
			{
				document.Comments = null;
			}

			if (document.Status == DocumentStatusEnums.NotIssue)
			{
				document.Status = DocumentStatusEnums.Issue;
			}
 
				// Update logic here
	      	var entity = await unitOfWork.Repository<Document>().UpdateAsync(document, cn, true);


			if (document.Status == DocumentStatusEnums.NotIssue)
			{
				document.Status = DocumentStatusEnums.Issue;

				var newDocumentReplay = new DocumentComment()
				{
					DocumentId = entity.Id.Value,
					Comment = "Issue",
					Status = DocumentStatusEnums.Issue
				};

				await unitOfWork.Repository<DocumentComment>().AddAsync(newDocumentReplay, cn, false, false, false);
			}

			if (document.Status == DocumentStatusEnums.ReplaySheet)
			{
				var newDocumentReplay = new DocumentComment()
				{
					DocumentId = entity.Id.Value,
					Comment = "ReplySheet",
					Status = DocumentStatusEnums.ReplaySheet
				};

				await unitOfWork.Repository<DocumentComment>().AddAsync(newDocumentReplay, cn, false, false, false);
			}
			return Ok(entity);
        }

        [HttpGet("[action]")]
        [ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
        public async Task<IActionResult> Delete(long id, CancellationToken cn)
        {
            // Delete logic here
            var model = unitOfWork.Repository<Document>().TableNoTracking.FirstOrDefault(c => c.Id == id);
            if (model != null)
                await unitOfWork.Repository<Document>().DeleteAsync(model, cn, true);
            return Ok();
        }

        [HttpGet("[action]")]
        [ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Custom)]
        public IActionResult Edit(long? id)
        {
            // Get logic here
            if (id != null && id != 0)
            {
                var entity = unitOfWork.Repository<Document>().TableNoTracking.FirstOrDefault(c => c.Id == id);
                return View(@"\Views\Panel\Edms\Document\Edit.cshtml", entity);
            }
            var newEntity = new Document();

            return View(@"\Views\Panel\Edms\Document\Edit.cshtml", newEntity);
        }
        [HttpGet("[action]")]
        [ActionDisplayName("ویرایش اطلاعات تکمیلی", ActionAccessType.View, ActionAccessItemType.Update)]
        public IActionResult EditAsGroup(long? id, long? projectId, long? documentVpisId)
        {
            Document? current = null;

            // Resolve group key from a specific revision id (if provided)
            if (id.HasValue && id.Value != 0)
            {
                current = unitOfWork.Repository<Document>().TableNoTracking
                    .FirstOrDefault(c => c.Id == id.Value);

                projectId = current?.ProjectId ?? projectId;
                documentVpisId = current?.DocumentVpisId ?? documentVpisId;
            }

            var vm = new DocumentGroupEditViewModel
            {
                ProjectId = projectId,
                DocumentVpisId = documentVpisId
            };

            if (projectId.HasValue && projectId.Value != 0)
            {
                vm.Project = unitOfWork.Repository<Project>().TableNoTracking.FirstOrDefault(p => p.Id == projectId.Value);
            }

            if (documentVpisId.HasValue && documentVpisId.Value != 0)
            {
                vm.DocumentVpis = unitOfWork.Repository<ProjectVpis>().TableNoTracking.FirstOrDefault(v => v.Id == documentVpisId.Value);
            }

            // If we have a valid group key, load all revisions
            if (projectId.HasValue && projectId.Value != 0 && documentVpisId.HasValue && documentVpisId.Value != 0)
            {
                var revisions = unitOfWork.Repository<Document>().TableNoTracking
                    .Where(d => d.ProjectId == projectId.Value && d.DocumentVpisId == documentVpisId.Value)
                    .OrderByDescending(d => d.Revision)
                    .Select(d => new DocumentRevisionSummaryViewModel
                    {
                        Id = d.Id ?? 0,
                        Revision = d.Revision,
                        Status = d.Status,
                        GoalOfProduction = d.GoalOfProduction,
                        PublicationShamsiDate = d.PublicationShamsiDate,
                        ModifiedDateShamsiDateTime = d.ModifiedDateShamsiDateTime,
                        MainFileId = d.MainFileId,
                        MotherFileId = d.MotherFileId,
                        SecondaryFileId = d.SecondaryFileId,
                        ReplySheetId = d.ReplySheetId,
                        CommentCount = 0
                    })
                    .ToList();

                // Comment counts (optional but improves UX in revision list)
                var revisionIds = revisions.Where(r => r.Id > 0).Select(r => r.Id).ToList();
                if (revisionIds.Count > 0)
                {
                    var counts = unitOfWork.Repository<DocumentComment>().TableNoTracking
                        .Where(c => revisionIds.Contains(c.DocumentId))
                        .GroupBy(c => c.DocumentId)
                        .Select(g => new { DocumentId = g.Key, Count = g.Count() })
                        .ToDictionary(x => x.DocumentId, x => x.Count);

                    foreach (var r in revisions)
                    {
                        if (r.Id > 0 && counts.TryGetValue(r.Id, out var count))
                        {
                            r.CommentCount = count;
                        }
                    }
                }

                vm.Revisions = revisions;

                // Pick current revision if not resolved yet: latest revision by default
                if (current == null)
                {
                    var latestId = revisions.FirstOrDefault(r => r.Id > 0)?.Id ?? 0;
                    if (latestId > 0)
                    {
                        current = unitOfWork.Repository<Document>().TableNoTracking
                            .FirstOrDefault(d => d.Id == latestId);
                    }
                }

                // If no revision exists yet, initialize a new document in this group (revision 0)
                if (current == null)
                {
                    current = new Document
                    {
                        ProjectId = projectId.Value,
                        DocumentVpisId = documentVpisId.Value,
                        Revision = 0,
                        Status = DocumentStatusEnums.NotReview
                    };
                }
            }

            // Attach comments for the current revision (so view can render the timeline/table)
            if (current?.Id.HasValue == true && current.Id.Value > 0)
            {
                current.Comments = unitOfWork.Repository<DocumentComment>().TableNoTracking
                         .Include(c=>c.HOLDOwner)
                    .Where(c => c.DocumentId == current.Id.Value)
                    .OrderByDescending(c => c.CreatedOnMiladiDateTime)
                    .ToList();
            }

            vm.Current = current ?? new Document();

               if (sdk.CurrentUser.IsAdministrator)
               {
                    //ViewData["CanEditDocument"] = true;

               }

               return View(@"\Views\Panel\Edms\Document\EditAsGroup.cshtml", vm);
        }
        [HttpGet("[action]")]
        [ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
        public IActionResult New()
        {
            var vm = new DocumentGroupEditViewModel();

			return View(@"\Views\Panel\Edms\Document\EditAsGroup.cshtml", vm);

			//return View(@"\Views\Panel\Edms\Document\Edit.cshtml", newEntity);
        }
        [HttpGet("[action]")]
        [ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
        public IActionResult List()
        {
            return View(@"\Views\Panel\Edms\Document\List.cshtml");
        }
        [HttpPost("[action]")]
        [ActionDisplayName("خروجی اکسل", ActionAccessType.Api)]
        public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
        {
            var licensePath = _webHostEnvironment.WebRootPath + "\\Aspose.Total.NET.lic";
            var memoryStream = new MemoryStream();
            try
            {
                await unitOfWork.Repository<Document>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);

                memoryStream.Position = 0;

                return File(memoryStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"exportExcel.xlsx");
            }
            catch (Exception ex)
            {

                return StatusCode(500, "خطا در زمان ایجاد فایل اکسل: " + ex.Message);
            }
        }
        [ActionDisplayName("دریافت اطلاعات", ActionAccessType.Api, ActionAccessItemType.FetchData)]
        [HttpPost("[action]")]
        public async Task<IActionResult> FetchData(DataTableRequest request, CancellationToken cn)
        {
            return Ok(await unitOfWork.Repository<Document>().FetchDataAsync(request, cn));
        }

		[HttpGet("[action]")]
		[ActionDisplayName("صفحه گزارش MDR", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult MdrReport()
		{
			return View(@"\Views\Panel\Edms\Document\MdrReport.cshtml");
		}

		[HttpGet("[action]")]
		[ActionDisplayName("گزارش MDR", ActionAccessType.Api, ActionAccessItemType.Custom)]
		public async Task<IActionResult> GetMdrReport([FromQuery] MdrReportFilter filter, CancellationToken cn)
		{
			return Ok(await mdrReportService.GetReportAsync(filter, cn));
		}

		[HttpPost("[action]")]
		[ActionDisplayName("گزارش MDR", ActionAccessType.Api, ActionAccessItemType.Custom)]
		public async Task<IActionResult> GetMdrReportPost([FromBody] MdrReportFilter filter, CancellationToken cn)
		{
			filter ??= new MdrReportFilter();
			return Ok(await mdrReportService.GetReportAsync(filter, cn));
		}

		[HttpPost("[action]")]
		[ActionDisplayName("خروجی اکسل MDR", ActionAccessType.Api, ActionAccessItemType.Custom)]
		public async Task<IActionResult> ExportMdrToExcel([FromBody] MdrReportFilter filter, CancellationToken cn)
		{
			var licensePath = _webHostEnvironment.WebRootPath + "\\Aspose.Total.NET.lic";
			var memoryStream = new MemoryStream();
			try
			{
				var report = await mdrReportService.GetReportAsync(filter ?? new MdrReportFilter(), cn);
				EdmsMdrReportExcelExporter.Export(report, memoryStream, licensePath);
				memoryStream.Position = 0;
				return File(memoryStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
					EdmsMdrReportExcelExporter.BuildFileName());
			}
			catch (Exception ex)
			{
				return StatusCode(500, "خطا در زمان ایجاد فایل اکسل: " + ex.Message);
			}
		}

		#region Personnel WorkLoad Report

		[HttpGet("[action]")]
		[ActionDisplayName("صفحه گزارش حجم کاری پرسنل", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult PersonnelWorkLoadReport()
		{
			return View(@"\Views\Panel\Edms\Document\PersonnelWorkLoadReport.cshtml");
		}

		[HttpPost("[action]")]
		[ActionDisplayName("گزارش حجم کاری پرسنل", ActionAccessType.Api, ActionAccessItemType.Custom)]
		public async Task<IActionResult> GetPersonnelWorkLoadReport([FromBody] PersonnelWorkLoadReportFilter filter, CancellationToken cn)
		{
			filter ??= new PersonnelWorkLoadReportFilter();

			if (!filter.ProjectId.HasValue && !filter.PersonnelId.HasValue)
				return BadRequest("حداقل یکی از فیلترهای پروژه یا پرسنل را انتخاب کنید.");

			return Ok(await BuildPersonnelWorkLoadReportAsync(filter, cn));
		}

		#endregion

		[HttpPost("[action]")]
		public async Task<IActionResult> AddNewComment(DocumentComment model, CancellationToken ct)
          {

			if (model.Status == null)
				throw new Exception("وضعیت مدرک انتخاب نشده است");
 

               model = await unitOfWork.Repository<DocumentComment>().AddAsync(model , ct,false);
			  

			return Ok(model);
          }

		[HttpPost("[action]")]
		public async Task<IActionResult> AddEditComment(DocumentComment model, CancellationToken ct)
		{

			if (model.Status == null)
				throw new Exception("وضعیت مدرک انتخاب نشده است");

			model.Comment += " *اصلاح* ";

			model = await unitOfWork.Repository<DocumentComment>().AddAsync(model, ct, false);
			  
			return Ok(model);
		}

		[HttpGet("[action]")]
        public IActionResult RevisionsListPartial(long projectId, long documentVpisId, long? currentId)
        {
            var revisions = unitOfWork.Repository<Document>().TableNoTracking
                .Where(d => d.ProjectId == projectId && d.DocumentVpisId == documentVpisId)
                .OrderByDescending(d => d.Revision)
                .Select(d => new DocumentRevisionSummaryViewModel
                {
                    Id = d.Id ?? 0,
                    Revision = d.Revision,
                    Status = d.Status,
                    GoalOfProduction = d.GoalOfProduction,
                    PublicationShamsiDate = d.PublicationShamsiDate,
                    ModifiedDateShamsiDateTime = d.ModifiedDateShamsiDateTime,
                    MainFileId = d.MainFileId,
                    MotherFileId = d.MotherFileId,
                    SecondaryFileId = d.SecondaryFileId,
                    ReplySheetId = d.ReplySheetId,
                    CommentCount = 0
                })
                .ToList();

            var revisionIds = revisions.Where(r => r.Id > 0).Select(r => r.Id).ToList();
            if (revisionIds.Count > 0)
            {
                var counts = unitOfWork.Repository<DocumentComment>().TableNoTracking
                    .Where(c => revisionIds.Contains(c.DocumentId))
                    .GroupBy(c => c.DocumentId)
                    .Select(g => new { DocumentId = g.Key, Count = g.Count() })
                    .ToDictionary(x => x.DocumentId, x => x.Count);

                foreach (var r in revisions)
                {
                    if (r.Id > 0 && counts.TryGetValue(r.Id, out var count))
                    {
                        r.CommentCount = count;
                    }
                }
            }

            ViewData["CurrentId"] = currentId ?? 0;
		  ViewData["RevisionsTotalCount"] = revisionIds?.Count ?? 0;
			return PartialView(@"\Views\Panel\Edms\Document\_DocumentRevisionsListPartial.cshtml", revisions);
        }

        [HttpGet("[action]")]
        public IActionResult RevisionEditorPartial(long id, bool canEditDocument = false)
        {
            var document = unitOfWork.Repository<Document>()
				.TableNoTracking
				.FirstOrDefault(d => d.Id == id);

            if (document?.Id.HasValue != true || document.Id.Value == 0)
            {
                return Content("<div class='alert alert-danger m-0'>ریویژن مورد نظر یافت نشد.</div>", "text/html; charset=utf-8");
            }

            document.Comments = unitOfWork.
				Repository<DocumentComment>().TableNoTracking
                    .Include(c=>c.HOLDOwner)
			 
                .Where(c => c.DocumentId == document.Id.Value)
                .OrderByDescending(c => c.CreatedOnMiladiDateTime)
                .ToList();

               ViewData["CanEditDocument"] = canEditDocument;


		  return PartialView(@"\Views\Panel\Edms\Document\_DocumentRevisionEditorPartial.cshtml", document);
        }

        [HttpGet("[action]")]
        public IActionResult NewRevisionEditorPartial(long projectId, long documentVpisId)
        {
            var maxRevision = unitOfWork.Repository<Document>().TableNoTracking
                .Where(d => d.ProjectId == projectId && d.DocumentVpisId == documentVpisId)
                .Select(d => (int?)d.Revision)
                .Max() ?? -1;

            var doc = new Document
            {
                ProjectId = projectId,
                DocumentVpisId = documentVpisId,
                Revision = maxRevision + 1,
                Status = DocumentStatusEnums.NotReview,
                Comments = new List<DocumentComment>()
            };

            return PartialView(@"\Views\Panel\Edms\Document\_DocumentRevisionEditorPartial.cshtml", doc);
        }

        [HttpGet("[action]")]
        public IActionResult DocumentCommentPartial(long? documentId , bool canEdit = false)
        {
            var model = new DocumentComment
            {
                DocumentId = documentId ?? 0
            };
               if(canEdit)
                    ViewData["CanEditDocument"] = true;



		  return PartialView(@"\Views\Panel\Edms\Document\_DocumentCommentPartial.cshtml", model);
        }

		[HttpGet("[action]")]
		public IActionResult DocumentCommentPartialEdit(long? documentId, DocumentStatusEnums currentStatus ,long projectId ,bool canEdit = false, bool editStatus = false)
		{
			var model = new DocumentComment
			{
				DocumentId = documentId ?? 0
			};
			if (canEdit)
				ViewData["CanEditDocument"] = true;

			

	   
			var currentUserIsDcc = CurrentUserHasRole("Edms.Documents.DccUsers");

			var project = unitOfWork.Repository<Project>()
				.TableNoTracking
				.FirstOrDefault(c => c.Id == projectId);

			var projectType = 0;

			if(project.ProjectIsVendoriType)
			{
				projectType = 1;
			}
			if (!editStatus)
			{
				ViewData["StatusOptions"] = GetFilteredStatusOptions(currentStatus, projectType, currentUserIsDcc);
			}
			else
			{
				ViewData["StatusOptions"] =  Enum.GetValues(typeof(DocumentStatusEnums))
				    .Cast<DocumentStatusEnums>().Select(e => new SelectListItem
				    {
					    Value = Convert.ToInt32(e).ToString(),
					    Text = e.ToString()
				    }).ToList();
			}



				return PartialView(@"\Views\Panel\Edms\Document\_DocumentCommentPartialEdit.cshtml", model);
		}

		[HttpGet("[action]")]
        public IActionResult DocumentVpisSummaryPartial(long documentVpisId)
        {
            var vpis = unitOfWork.Repository<ProjectVpis>().TableNoTracking
                .FirstOrDefault(v => v.Id == documentVpisId);

            if (vpis == null)
            {
                return Content("<div class='alert alert-warning m-0'>سند (Vpis) انتخاب‌شده یافت نشد.</div>", "text/html; charset=utf-8");
            }

            return PartialView(@"\Views\Panel\Edms\Document\_DocumentVpisSummaryPartial.cshtml", vpis);
        }
		#region DocumentProductSection

		//[HttpGet("DocumentProducts/List")]
		//[ActionDisplayName("مدارک محصولات", ActionAccessType.View, ActionAccessItemType.Custom)]
		//public IActionResult DocumentProductList()
		//{
		//	return View(@"\Views\Panel\Edms\Document\DocumentProducts\List.cshtml");
		//}

		[HttpGet("DocumentProducts/ListDocumentProduct")]
		[ActionDisplayName("مدارک محصولات", ActionAccessType.View, ActionAccessItemType.Custom)]
		public IActionResult ListDocumentProduct()
		{
			return View(@"\Views\Panel\Edms\Document\DocumentProducts\ListDocumentProduct.cshtml");
		}

		[HttpPost("[action]")]
		[ActionDisplayName("دریافت اطلاعات مدارک محصولات", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> DocumentProductFetchData(DataTableRequest request, CancellationToken cn)
		{
			var productFormulRepo = unitOfWork.Repository<ProductFormul>().TableNoTracking;
			var partRepo = unitOfWork.Repository<Part>().TableNoTracking;
			var productGroupRepo = unitOfWork.Repository<ProductGroup>().TableNoTracking;
			var partDocumentRepo = unitOfWork.Repository<PartDocument>().TableNoTracking;
			var inquiryPartPriceRepo = unitOfWork.Repository<InquiryPartPrice>().TableNoTracking;
               var partExteraInfo = unitOfWork.Repository<PartExtraInfo>().TableNoTracking;

			var lastPartExtraInfo = partExteraInfo
                        .GroupBy(x => x.ProductId)
                        .Select(g => new
                        {
	                        ProductId = g.Key,
	                        Revision = g.Max(x => x.Revision)
                        });


			var partExtraInfoLatest =
                   from pe in partExteraInfo
                   join last in lastPartExtraInfo
	                  on new { pe.ProductId, pe.Revision }
	                  equals new { last.ProductId, last.Revision }
                   select pe;


			// Phase 1: Base query بدون let (سریع) - فقط join ساده
			var baseQuery = productFormulRepo
    .Join(
	   partRepo,
	   pf => pf.ProductId,
	   part => part.Id,
	   (pf, part) => new { pf, part }
    )
    .LeftJoin(
	   partExtraInfoLatest,
	   x => x.part.Id,
	   pe => pe.ProductId,
	   (x, pe) => new
	   {
		   x.pf,
		   x.part,
		   pe,
		   PartExteraInfoRev = pe != null ? pe.Revision : null,
		   PartExteraInfoType = pe != null ? pe.InfoType : null
	   }
    )
    .LeftJoin(
	   productGroupRepo,
	   x => x.pf.ProductNameGroupId,
	   pg => pg.Id,
	   (x, pg) => new
	   {
		   x.pf.Id,
		   x.pf.ProductId,
		   x.pf.ProductNameGroupId,
		   x.pf.CreatedOnMiladiDateTime,
		   x.pf.Comment,
		   ProductNameGroupName = pg != null ? pg.Name : null,
		   PartCode = x.part.Code,
		   PartName = x.part.Name,
		   IsRoutine = x.part.DesignTypeIsRoutine,
		   Foreign = x.part.Foreign,
		   EngineeringRoutine = x.part.EngineeringRoutine,
		   DataSheetUsage = x.part.DataSheetUsage,
		   DataSheet = x.part.DataSheet,
		   BrandInDataSheet = x.part.BrandInDataSheet,
		   PreciseTool = x.part.PreciseTool,
		   x.PartExteraInfoType,
		   x.PartExteraInfoRev
	   }
    );



			// فیلترهای ستونی - برای ستون‌های وابسته به subquery، ابتدا IDها را می‌گیریم
			if (request.columns != null)
			{
				foreach (var col in request.columns.Where(c => c.searchable && c.Search?.value != null && c.Search.value.Length > 0))
				{
					var searchVal = col.Search!.value[0] ?? "";
					if (string.IsNullOrWhiteSpace(searchVal)) continue;

					var searchLower = searchVal.Trim().ToLower();
					var colData = (col.name ?? col.data ?? "").Trim();

					switch (colData)
					{
						case "PartCode": baseQuery = baseQuery.Where(x => x.PartCode != null && x.PartCode.ToLower().Contains(searchLower)); break;
						case "PartName": baseQuery = baseQuery.Where(x => x.PartName != null && x.PartName.ToLower().Contains(searchLower)); break;
						case "ProductNameGroupName": baseQuery = baseQuery.Where(x => x.ProductNameGroupName != null && x.ProductNameGroupName.ToLower().Contains(searchLower)); break;
						case "Comment": baseQuery = baseQuery.Where(x => x.Comment != null && x.Comment.ToLower().Contains(searchLower)); break;
						case "Id":
						case "ProductFormul_ID": if (long.TryParse(searchVal, out var idVal)) baseQuery = baseQuery.Where(x => x.Id == idVal); break;
						case "PartId": if (long.TryParse(searchVal, out var pidVal)) baseQuery = baseQuery.Where(x => x.ProductId == pidVal); break;
						case "ProductNameGroupId": if (long.TryParse(searchVal, out var gidVal)) baseQuery = baseQuery.Where(x => x.ProductNameGroupId == gidVal); break;
						case "Foreign": var f = searchLower == "بله" || searchLower == "true" || searchLower == "1"; baseQuery = baseQuery.Where(x => f ? x.Foreign : !x.Foreign); break;
						case "EngineeringRoutine": var er = searchLower == "بله" || searchLower == "true" || searchLower == "1"; baseQuery = baseQuery.Where(x => er ? x.EngineeringRoutine : !x.EngineeringRoutine); break;
						case "PreciseTool": var pt = searchLower == "بله" || searchLower == "true" || searchLower == "1"; baseQuery = baseQuery.Where(x => pt ? x.PreciseTool : !x.PreciseTool); break;
						case "CreatedDate": baseQuery = baseQuery.Where(x => x.CreatedOnMiladiDateTime != null && x.CreatedOnMiladiDateTime.Value.ToString("yyyy-MM-dd").Contains(searchVal)); break;
						case "SalePrice":
						case "BuyPrice":
							if (decimal.TryParse(searchVal, out var bpVal))
							{
								var productIds = await db.vw_PartDocumentPrices.AsNoTracking()
									.Where(v => v.BuyPrice == bpVal && v.ProductId != null).Select(v => v.ProductId!.Value).Distinct().ToListAsync(cn);
								if (productIds.Count == 0) baseQuery = baseQuery.Where(_ => false);
								else baseQuery = baseQuery.Where(x => productIds.Contains(x.ProductId));
							}
							break;
						case "InternalTotalBuyPrice":
							if (long.TryParse(searchVal, out var itVal))
							{
								var productIdsIt = await db.vw_PartDocumentPrices.AsNoTracking()
									.Where(v => v.InternalTotalBuyPrice == itVal && v.ProductId != null).Select(v => v.ProductId!.Value).Distinct().ToListAsync(cn);
								if (productIdsIt.Count == 0) baseQuery = baseQuery.Where(_ => false);
								else baseQuery = baseQuery.Where(x => productIdsIt.Contains(x.ProductId));
							}
							break;
						case "ExternalTotalBuyPrice":
							if (long.TryParse(searchVal, out var etVal))
							{
								var productIdsEt = await db.vw_PartDocumentPrices.AsNoTracking()
									.Where(v => v.ExternalTotalBuyPrice == etVal && v.ProductId != null).Select(v => v.ProductId!.Value).Distinct().ToListAsync(cn);
								if (productIdsEt.Count == 0) baseQuery = baseQuery.Where(_ => false);
								else baseQuery = baseQuery.Where(x => productIdsEt.Contains(x.ProductId));
							}
							break;
						case "BuyPriceInEuro":
							if (decimal.TryParse(searchVal, out var beVal))
							{
								var partIdsEuro = await inquiryPartPriceRepo
									.Where(ip => ip.Type != InquiryPartPriceTypeEnum.AfterSalesInquiry && ip.CurrencyActualPrice == beVal)
									.Select(ip => ip.PartId).Distinct().ToListAsync(cn);
								if (partIdsEuro.Count == 0) baseQuery = baseQuery.Where(_ => false);
								else baseQuery = baseQuery.Where(x => partIdsEuro.Contains(x.ProductId));
							}
							break;
						case "HaveAttachment":
							var ha = searchLower == "بله" || searchLower == "true" || searchLower == "1";
							var partIdsWithAtt = await partDocumentRepo.Select(pd => pd.PartId).Distinct().ToListAsync(cn);
							var partIdsWithAttSet = partIdsWithAtt.ToHashSet();
							baseQuery = ha ? baseQuery.Where(x => partIdsWithAttSet.Contains(x.ProductId)) : baseQuery.Where(x => !partIdsWithAttSet.Contains(x.ProductId));
							break;
						case "HasBom":
							var hb = searchLower == "بله" || searchLower == "true" || searchLower == "1";
							var pfIdsWithBom = await db.vw_ProductItems.AsNoTracking().Select(pi => pi.ProductFormulId).Distinct().ToListAsync(cn);
							baseQuery = hb ? baseQuery.Where(x => pfIdsWithBom.Contains((long)x.Id)) : baseQuery.Where(x => !pfIdsWithBom.Contains((long)x.Id));
							break;
						case "BomProductFormulAttachmentCount":
							if (int.TryParse(searchVal, out var bcVal))
							{
								var partIdsByCount = await partDocumentRepo.Where(pd => pd.Main == true)
									.GroupBy(pd => pd.PartId).Where(g => g.Count() == bcVal).Select(g => g.Key).ToListAsync(cn);
								if (partIdsByCount.Count == 0) baseQuery = baseQuery.Where(_ => false);
								else baseQuery = baseQuery.Where(x => partIdsByCount.Contains(x.ProductId));
							}
							break;
					}
				}
			}

			// Global search
			if (request.search?.value != null && request.search.value.Length > 0)
			{
				var globalSearch = request.search.value[0]?.Trim();
				if (!string.IsNullOrEmpty(globalSearch))
				{
					var gs = globalSearch.ToLower();
					baseQuery = baseQuery.Where(x =>
						(x.PartCode != null && x.PartCode.ToLower().Contains(gs)) ||
						(x.PartName != null && x.PartName.ToLower().Contains(gs)) ||
						(x.ProductNameGroupName != null && x.ProductNameGroupName.ToLower().Contains(gs)) ||
						(x.Comment != null && x.Comment.ToLower().Contains(gs)));
				}
			}

			var recordsTotal = await (from pf in productFormulRepo join part in partRepo on pf.ProductId equals part.Id select 1).CountAsync(cn);
			var recordsFiltered = await baseQuery.CountAsync(cn);

			// Ordering (فقط ستون‌های base)
			var orderColData = "Id";
			var isDesc = true;
			if (request.order != null && request.order.Length > 0 && request.columns != null)
			{
				var colIndex = int.TryParse(request.order[0].column, out var idx) ? idx : 0;
				if (colIndex >= 0 && colIndex < request.columns.Count)
				{
					orderColData = request.columns[colIndex].name ?? request.columns[colIndex].data ?? "Id";
					isDesc = request.order[0].dir?.ToLowerInvariant() == "desc";
				}
			}

			var orderedQuery = orderColData switch
			{
				"PartCode" => isDesc ? baseQuery.OrderByDescending(x => x.PartCode) : baseQuery.OrderBy(x => x.PartCode),
				"PartName" => isDesc ? baseQuery.OrderByDescending(x => x.PartName) : baseQuery.OrderBy(x => x.PartName),
				"ProductNameGroupName" => isDesc ? baseQuery.OrderByDescending(x => x.ProductNameGroupName) : baseQuery.OrderBy(x => x.ProductNameGroupName),
				"Comment" => isDesc ? baseQuery.OrderByDescending(x => x.Comment) : baseQuery.OrderBy(x => x.Comment),
				"CreatedDate" => isDesc ? baseQuery.OrderByDescending(x => x.CreatedOnMiladiDateTime) : baseQuery.OrderBy(x => x.CreatedOnMiladiDateTime),
				_ => baseQuery.OrderByDescending(x => x.Id)
			};

			var pageBase = await orderedQuery.Skip(request.start).Take(request.length).ToListAsync(cn);
			if (pageBase.Count == 0)
				return Ok(new DataTableResponse { Draw = request.draw, RecordsTotal = recordsTotal, RecordsFiltered = recordsFiltered, Data = new List<DocumentProductListDto>() });

			var productIdsPage = pageBase.Select(x => x.ProductId).Distinct().ToList();
			var formulIdsPage = pageBase.Select(x => x.Id).ToList();

			// Phase 2: بارگذاری داده‌های کمکی فقط برای صفحه فعلی
			var priceDict = await db.vw_PartDocumentPrices.AsNoTracking()
				.Where(v => v.ProductId != null && productIdsPage.Contains(v.ProductId!.Value))
				.GroupBy(v => v.ProductId)
				.Select(g => new { ProductId = g.Key, First = g.First() })
				.ToDictionaryAsync(x => x.ProductId!.Value, x => x.First, cn);

			var lastPrices = await inquiryPartPriceRepo
				.Where(ip => productIdsPage.Contains(ip.PartId) && ip.Type != InquiryPartPriceTypeEnum.AfterSalesInquiry)
				.GroupBy(ip => ip.PartId)
				.Select(g => new { PartId = g.Key, Price = g.OrderByDescending(x => x.CreatedOnMiladiDateTime).Select(x => (decimal?)x.CurrencyActualPrice).FirstOrDefault() })
				.ToDictionaryAsync(x => x.PartId, x => x.Price, cn);

			var attachmentCounts = await partDocumentRepo.Where(pd => productIdsPage.Contains(pd.PartId) && pd.Main == true)
				.GroupBy(pd => pd.PartId)
				.Select(g => new { PartId = g.Key, Count = g.Count() })
				.ToDictionaryAsync(x => x.PartId, x => x.Count, cn);

			var hasAttachmentList = await partDocumentRepo.Where(pd => productIdsPage.Contains(pd.PartId))
				.Select(pd => pd.PartId).Distinct().ToListAsync(cn);
			var hasAttachmentSet = hasAttachmentList.ToHashSet();

			var hasBomList = await db.vw_ProductItems.AsNoTracking()
				.Where(pi => formulIdsPage.Contains(pi.ProductFormulId))
				.Select(pi => pi.ProductFormulId).Distinct().ToListAsync(cn);
			var hasBomSet = hasBomList.ToHashSet();

			var data = pageBase.Select(x =>
			{
				var price = priceDict.GetValueOrDefault(x.ProductId);
				return new DocumentProductListDto
				{
					Id = x.Id,
					ProductFormulId = x.Id,
					ProductFormul_ID = x.Id,
					HaveAttachment = hasAttachmentSet.Contains(x.ProductId),
					PartId = x.ProductId,
					ProductNameGroupId = x.ProductNameGroupId,
					ProductNameGroupName = x.ProductNameGroupName,
					CreatedDate = x.CreatedOnMiladiDateTime,
					Comment = x.Comment,
					PartCode = x.PartCode,
					PartName = x.PartName,
					IsRoutine = x.IsRoutine,
					SalePrice = price?.BuyPrice,
					BuyPrice = price?.BuyPrice,
					InternalTotalBuyPrice = price?.InternalTotalBuyPrice,
					ExternalTotalBuyPrice = price?.ExternalTotalBuyPrice,
					BuyPriceInEuro = lastPrices.GetValueOrDefault(x.ProductId),
					BomProductFormulAttachmentCount = attachmentCounts.GetValueOrDefault(x.ProductId, 0),
					HasBom = hasBomSet.Contains((long)x.Id),
					Foreign = x.Foreign,
					EngineeringRoutine = x.EngineeringRoutine,
					DataSheetUsage = x.DataSheetUsage,
					DataSheet = x.DataSheet,
					BrandInDataSheet = x.BrandInDataSheet,
					PreciseTool = x.PreciseTool,
					PartExteraInfoRev =x.PartExteraInfoRev,
					PartExteraInfoType =x.PartExteraInfoType
				};
			}).ToList();

			return Ok(new DataTableResponse
			{
				Draw = request.draw,
				RecordsTotal = recordsTotal,
				RecordsFiltered = recordsFiltered,
				Data = data
			});
		}
		#endregion

		#region Personnel WorkLoad Report Helpers

		public sealed class PersonnelWorkLoadReportFilter
		{
			public long? ProjectId { get; set; }
			public long? PersonnelId { get; set; }
		}

		public sealed class PersonnelWorkLoadReportResponse
		{
			public List<PersonnelWorkLoadReportItemDto> Items { get; set; } = [];
		}

		public sealed class PersonnelWorkLoadReportItemDto
		{
			public long ProjectId { get; set; }
			public string? ProjectCode { get; set; }
			public string? ProjectName { get; set; }
			public long PersonnelId { get; set; }
			public string? PersonnelName { get; set; }
			public int Rev0ManHour { get; set; }
			public int Rev1ManHour { get; set; }
			public int RevNManHour { get; set; }
			public int CheckingManHour { get; set; }
			public int ManHour1MonthsLater { get; set; }
			public int ManHour2MonthsLater { get; set; }
			public int ManHourNMonthsLater { get; set; }
			public List<PersonnelWorkLoadProductionDateDto> ProductionDates { get; set; } = [];
		}

		public sealed class PersonnelWorkLoadProductionDateDto
		{
			public string? DocumentNumber { get; set; }
			public string? DocumentTitle { get; set; }
			public int Revision { get; set; }
			public string? ProductionDate { get; set; }
			public int ManHour { get; set; }
		}

		private sealed class PersonnelWorkLoadVpisInfo
		{
			public long VpisId { get; init; }
			public string? VpisCode { get; init; }
			public string? VpisTitle { get; init; }
			public long ProjectId { get; init; }
			public string? ProjectCode { get; init; }
			public string? ProjectName { get; init; }
			public int HavayarDelayDays { get; init; }
			public int ClientDelayDays { get; init; }
			public long ProducerId { get; init; }
			public string? ProducerName { get; init; }
			public int Rev0ManHour { get; init; }
			public int Rev1ManHour { get; init; }
			public int Rev2ManHour { get; init; }
			public int Rev3ManHour { get; init; }
		}

		private sealed class PersonnelWorkLoadDocumentInfo
		{
			public long DocumentId { get; init; }
			public long VpisId { get; init; }
			public string? VpisCode { get; init; }
			public string? VpisTitle { get; init; }
			public long ProjectId { get; init; }
			public string? ProjectCode { get; init; }
			public string? ProjectName { get; init; }
			public long PersonnelId { get; init; }
			public string? PersonnelName { get; init; }
			public int Revision { get; init; }
			public int RevisionManHour { get; init; }
			public int ConsumedManHour { get; init; }
			public DateTime ProductionDate { get; init; }
		}

		private async Task<PersonnelWorkLoadReportResponse> BuildPersonnelWorkLoadReportAsync(
			PersonnelWorkLoadReportFilter filter,
			CancellationToken cancellationToken)
		{
			var excludedProjectStatuses = new[] { ProjectStatusEnum.StartNotStarted, ProjectStatusEnum.Completed };

			var vpisQuery =
				from vpis in db.Set<ProjectVpis>().AsNoTracking()
				join project in db.Set<Project>().AsNoTracking() on vpis.ProjectNameId equals project.Id
				join producer in db.Set<User>().AsNoTracking() on vpis.ProducerId equals producer.Id
				join party in db.Set<Party>().AsNoTracking() on producer.PartyId equals party.Id into partyJoin
				from party in partyJoin.DefaultIfEmpty()
				where vpis.Id.HasValue
				      && project.Id.HasValue
				      && producer.Id.HasValue
				      && vpis.IsActive != IsActiveEnum.Deleted
				      && project.IsActive != IsActiveEnum.Deleted
				      && producer.IsActive != IsActiveEnum.Deleted
				      && !excludedProjectStatuses.Contains(project.Status)
				select new PersonnelWorkLoadVpisInfo
				{
					VpisId = vpis.Id!.Value,
					VpisCode = vpis.Code,
					VpisTitle = vpis.Title,
					ProjectId = project.Id!.Value,
					ProjectCode = project.Code,
					ProjectName = project.Title,
					HavayarDelayDays = project.LegalDelayHavayar ?? 0,
					ClientDelayDays = project.LegalClientDayDelay ?? 0,
					ProducerId = producer.Id!.Value,
					ProducerName = !string.IsNullOrWhiteSpace(party.FullNameInEnglish)
						? party.FullNameInEnglish
						: !string.IsNullOrWhiteSpace(party.FullName)
							? party.FullName
							: producer.Name,
					Rev0ManHour = vpis.PersonHourRevisionZero ?? 0,
					Rev1ManHour = vpis.PersonHourRevisionOne ?? 0,
					Rev2ManHour = vpis.PersonHourRevisionTwo ?? 0,
					Rev3ManHour = vpis.PersonHourRevisionThree ?? 0
				};

			if (filter.ProjectId.HasValue)
				vpisQuery = vpisQuery.Where(x => x.ProjectId == filter.ProjectId.Value);

			if (filter.PersonnelId.HasValue)
				vpisQuery = vpisQuery.Where(x => x.ProducerId == filter.PersonnelId.Value);

			var vpisList = await vpisQuery.ToListAsync(cancellationToken);
			if (vpisList.Count == 0)
				return new PersonnelWorkLoadReportResponse();

			var vpisById = vpisList.ToDictionary(v => v.VpisId);
			var vpisIds = vpisById.Keys.ToList();

			var documents = await db.Set<Document>().AsNoTracking()
				.Where(d => d.Id.HasValue
				            && d.DocumentVpisId.HasValue
				            && vpisIds.Contains(d.DocumentVpisId.Value)
				            && d.IsActive != IsActiveEnum.Deleted)
				.OrderBy(d => d.Revision)
				.ThenBy(d => d.Id)
				.ToListAsync(cancellationToken);

			if (documents.Count == 0)
				return new PersonnelWorkLoadReportResponse();

			var documentIds = documents.Select(d => d.Id!.Value).ToList();
			var comments = await db.Set<DocumentComment>().AsNoTracking()
				.Where(c => documentIds.Contains(c.DocumentId) && c.IsActive != IsActiveEnum.Deleted)
				.OrderBy(c => c.Id)
				.ToListAsync(cancellationToken);

			var commentsByDocument = comments.GroupBy(c => c.DocumentId).ToDictionary(g => g.Key, g => g.ToList());
			var reportDocuments = BuildPersonnelWorkLoadDocuments(documents, commentsByDocument, vpisById);

			var items = reportDocuments
				.GroupBy(d => new { d.ProjectId, d.PersonnelId })
				.Select(group => BuildPersonnelWorkLoadItem(group))
				.OrderBy(i => i.ProjectCode)
				.ThenBy(i => i.PersonnelName)
				.ToList();

			return new PersonnelWorkLoadReportResponse { Items = items };
		}

		private static List<PersonnelWorkLoadDocumentInfo> BuildPersonnelWorkLoadDocuments(
			List<Document> documents,
			Dictionary<long, List<DocumentComment>> commentsByDocument,
			Dictionary<long, PersonnelWorkLoadVpisInfo> vpisById)
		{
			var validStatuses = new HashSet<DocumentStatusEnums>
			{
				DocumentStatusEnums.CommentedByEmployer,
				DocumentStatusEnums.ApprovedAsNoteByEmployer,
				DocumentStatusEnums.NotReview
			};

			var result = new List<PersonnelWorkLoadDocumentInfo>();

			foreach (var document in documents)
			{
				if (!document.Id.HasValue || !document.DocumentVpisId.HasValue)
					continue;

				if (!vpisById.TryGetValue(document.DocumentVpisId.Value, out var vpis))
					continue;

				var lastComment = commentsByDocument.GetValueOrDefault(document.Id.Value)?.LastOrDefault();
				if (lastComment?.Status is not { } lastStatus || !validStatuses.Contains(lastStatus))
					continue;

				var commentDate = lastComment.CreatedOnMiladiDateTime
				                  ?? document.ModifiedDateMiladiDateTime
				                  ?? document.CreatedOnMiladiDateTime;
				if (!commentDate.HasValue)
					continue;

				var totalClientDelay = vpis.HavayarDelayDays + vpis.ClientDelayDays;
				var productionDate = lastStatus == DocumentStatusEnums.NotReview
					? commentDate.Value.AddDays(totalClientDelay)
					: commentDate.Value.AddDays(vpis.HavayarDelayDays);

				result.Add(new PersonnelWorkLoadDocumentInfo
				{
					DocumentId = document.Id.Value,
					VpisId = vpis.VpisId,
					VpisCode = vpis.VpisCode,
					VpisTitle = vpis.VpisTitle,
					ProjectId = vpis.ProjectId,
					ProjectCode = vpis.ProjectCode,
					ProjectName = vpis.ProjectName,
					PersonnelId = vpis.ProducerId,
					PersonnelName = vpis.ProducerName,
					Revision = document.Revision,
					RevisionManHour = GetRevisionManHour(vpis, document.Revision),
					ConsumedManHour = Math.Max(0, document.HourPrePerson ?? 0),
					ProductionDate = productionDate
				});
			}

			return result;
		}

		private static PersonnelWorkLoadReportItemDto BuildPersonnelWorkLoadItem(
			IEnumerable<PersonnelWorkLoadDocumentInfo> group)
		{
			var documents = group.ToList();
			var first = documents[0];
			var checkingManHour = CalculateCheckingManHour(documents);
			var calculated = CalculatePeriodManHours(documents, checkingManHour);

			return new PersonnelWorkLoadReportItemDto
			{
				ProjectId = first.ProjectId,
				ProjectCode = first.ProjectCode,
				ProjectName = first.ProjectName,
				PersonnelId = first.PersonnelId,
				PersonnelName = first.PersonnelName,
				Rev0ManHour = documents.Where(d => d.Revision <= 0).Sum(d => d.RevisionManHour),
				Rev1ManHour = documents.Where(d => d.Revision == 1).Sum(d => d.RevisionManHour),
				RevNManHour = documents.Where(d => d.Revision > 1).Sum(d => d.RevisionManHour),
				CheckingManHour = checkingManHour,
				ManHour1MonthsLater = calculated.OneMonth,
				ManHour2MonthsLater = calculated.TwoMonths,
				ManHourNMonthsLater = calculated.RestOfYear,
				ProductionDates = documents
					.OrderBy(d => d.ProductionDate)
					.ThenBy(d => d.VpisCode)
					.Select(d => new PersonnelWorkLoadProductionDateDto
					{
						DocumentNumber = d.VpisCode,
						DocumentTitle = d.VpisTitle,
						Revision = d.Revision,
						ProductionDate = d.ProductionDate.ToShamsiDate(),
						ManHour = d.RevisionManHour
					})
					.ToList()
			};
		}

		private static int GetRevisionManHour(PersonnelWorkLoadVpisInfo vpis, int revision)
		{
			return revision switch
			{
				<= 0 => vpis.Rev0ManHour,
				1 => vpis.Rev1ManHour,
				2 => vpis.Rev2ManHour,
				_ => vpis.Rev3ManHour > 0 ? vpis.Rev3ManHour : vpis.Rev2ManHour
			};
		}

		private static int CalculateCheckingManHour(List<PersonnelWorkLoadDocumentInfo> documents)
		{
			var lastRevisionConsumedManHours = documents
				.GroupBy(d => d.VpisId)
				.Select(g => g
					.OrderByDescending(d => d.Revision)
					.ThenByDescending(d => d.DocumentId)
					.First().ConsumedManHour)
				.Sum();

			return (int)Math.Round(lastRevisionConsumedManHours * 0.2m, MidpointRounding.AwayFromZero);
		}

		private static (int OneMonth, int TwoMonths, int RestOfYear) CalculatePeriodManHours(
			List<PersonnelWorkLoadDocumentInfo> documents,
			int checkingManHour)
		{
			if (documents.Count == 0)
				return (0, 0, 0);

			var now = DateTime.Now;
			var oneMonthLimit = now.AddMonths(1);
			var twoMonthsLimit = oneMonthLimit.AddMonths(1);
			var yearLimit = new DateTime(oneMonthLimit.Year, 12, 31, 23, 59, 59);

			var oneMonth = 0;
			var twoMonths = 0;
			var restOfYear = 0;

			foreach (var document in documents)
			{
				if (document.ProductionDate <= oneMonthLimit)
				{
					oneMonth += document.RevisionManHour;
					continue;
				}

				if (document.ProductionDate <= twoMonthsLimit)
				{
					twoMonths += document.RevisionManHour;
					continue;
				}

				if (document.ProductionDate <= yearLimit)
					restOfYear += document.RevisionManHour;
			}

			return (
				oneMonth > 0 ? oneMonth + checkingManHour : 0,
				twoMonths > 0 ? twoMonths + checkingManHour : 0,
				restOfYear > 0 ? restOfYear + checkingManHour : 0);
		}

		#endregion

		public static List<SelectListItem> GetFilteredStatusOptions(DocumentStatusEnums currentStatus, int? projectType = null , bool isDccUser = false)
		{
			  
			var enumType = typeof(DocumentStatusEnums);
			var items = Enum.GetValues(enumType)
			    .Cast<DocumentStatusEnums>();

			var filtered = new List<SelectListItem>();
			// 0 = Sp Project

			if(isDccUser)
			{
				filtered = items.Where(item =>
				{
					if (currentStatus == DocumentStatusEnums.NotIssue)
						return item == DocumentStatusEnums.Issue;

					if (currentStatus == DocumentStatusEnums.Issue || currentStatus == DocumentStatusEnums.ReviewIssuer)
						return item == DocumentStatusEnums.ApproveByReviewer
						|| item == DocumentStatusEnums.RejectByReviewer
						|| item == DocumentStatusEnums.CommentedByReviewer;

					if (currentStatus == DocumentStatusEnums.ApproveByReviewer)
						return item == DocumentStatusEnums.ApproveByApprover
						|| item == DocumentStatusEnums.RejectByApprover
						|| item == DocumentStatusEnums.CommentedByApprover;

					if (currentStatus == DocumentStatusEnums.ApproveByApprover)
						return item == DocumentStatusEnums.ApprovedByDcc 
						   || item == DocumentStatusEnums.CommentedByDcc
						   || item == DocumentStatusEnums.RejectByDcc;

					if (currentStatus == DocumentStatusEnums.NotReview || item == DocumentStatusEnums.ApprovedByDcc)
						return item == DocumentStatusEnums.ApproveByEmployer
						|| item == DocumentStatusEnums.RejectByEmployer
						|| item == DocumentStatusEnums.CommentedByEmployer
						|| item == DocumentStatusEnums.ApprovedAsNoteByEmployer
						|| item == DocumentStatusEnums.ReIssued;

					if (currentStatus == DocumentStatusEnums.ClientApproval)
						return item == DocumentStatusEnums.ReIssued;
						 

					return false;

				}).Select(e => new SelectListItem
				{
					Value = Convert.ToInt32(e).ToString(),
					Text = e.ToString()
				}).ToList();
			}
			else if (projectType == 0)
			{

	 
				  filtered = items.Where(item =>
				{
					if (currentStatus == DocumentStatusEnums.NotIssue)
						return item == DocumentStatusEnums.Issue;
					
					if (currentStatus == DocumentStatusEnums.Issue || currentStatus == DocumentStatusEnums.ReviewIssuer)
						return item == DocumentStatusEnums.ApproveByReviewer 
						|| item == DocumentStatusEnums.RejectByReviewer 
						|| item == DocumentStatusEnums.CommentedByReviewer;

					if (currentStatus == DocumentStatusEnums.ApproveByReviewer )
						return item == DocumentStatusEnums.ApproveByApprover 
						|| item == DocumentStatusEnums.RejectByApprover 
						|| item == DocumentStatusEnums.CommentedByApprover;

					

					return false;
				}).Select(e => new SelectListItem
				{
					Value = Convert.ToInt32(e).ToString(),
					Text = e.ToString()
				}).ToList();
			}
			else
			{
				// 1 = Vp Project

				filtered = items.Where(item =>
				{

					if (currentStatus == DocumentStatusEnums.NotIssue)
						return item == DocumentStatusEnums.Issue;

					if (currentStatus == DocumentStatusEnums.Issue || currentStatus == DocumentStatusEnums.ReviewIssuer)
						return item == DocumentStatusEnums.ApproveByReviewer
						|| item == DocumentStatusEnums.RejectByReviewer
						|| item == DocumentStatusEnums.CommentedByReviewer;

					if (currentStatus == DocumentStatusEnums.ApproveByReviewer)
						return item == DocumentStatusEnums.ApproveByApprover
						|| item == DocumentStatusEnums.RejectByApprover
						|| item == DocumentStatusEnums.CommentedByApprover;

					return false;
				}).Select(e => new SelectListItem
				{
					Value = Convert.ToInt32(e).ToString(),
					Text = e.ToString()
				}).ToList();

			}

			 
		 
			return filtered;
		}
	}
}
