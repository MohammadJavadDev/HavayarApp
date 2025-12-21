using Common.Attributes;
using Common.Auth.Enums;
using Data.Contracts;
using Data.SystemAuth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Entities.Base.DataTable;
using WebFramework.Filtters;
using WebFramework.Page;
using Entities.App.Edms;
using Entities.App.Edms.Enums;
using WebApp.ViewModels.Edms;

namespace WebApp.Controllers.Dynamic
{
	[Route("Panel/Edms/[controller]")]
	[ApiController]
    [ApiResultFilter]
    [ControllerInfo("مدارک مهندسی", typeof(Document))]
    public class DocumentController(IUnitOfWork unitOfWork, IPropertyIdentityService identityService, IWebHostEnvironment _webHostEnvironment) : BaseController
    {
        [HttpPost("[action]/{CanEditDocument?}")]
        [ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
        public async Task<IActionResult> Save(Document document,bool? CanEditDocument , CancellationToken cn)
        {
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

			// Update logic here
			var entity = await unitOfWork.Repository<Document>().UpdateAsync(document, cn, true);
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
		[HttpPost("[action]")]
		public async Task<IActionResult> AddNewComment(DocumentComment model, CancellationToken ct)
          {


               model = await unitOfWork.Repository<DocumentComment>().AddAsync(model , ct,false,false);


               await unitOfWork.Repository<Document>().UpdateFieldsAsync(
				model.DocumentId,
                    c=>c.Status,
				model.Status,
                    ct
				);

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
            var document = unitOfWork.Repository<Document>().TableNoTracking.FirstOrDefault(d => d.Id == id);
            if (document?.Id.HasValue != true || document.Id.Value == 0)
            {
                return Content("<div class='alert alert-danger m-0'>ریویژن مورد نظر یافت نشد.</div>", "text/html; charset=utf-8");
            }

            document.Comments = unitOfWork.Repository<DocumentComment>().TableNoTracking
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

    }
}
