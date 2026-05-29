using Common.Attributes;
using Common.Auth.Enums;
using Data;
using Data.Contracts;
using Entities.App.Eng;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.Dynamic.Eng
{
    [Route("Panel/Eng/[controller]")]
    [ApiController]
    [ApiResultFilter]
    [ControllerInfo("پارت لیست مصرفی", typeof(PartList))]
    public class ProjectUtilizedMaterialController : BaseController
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ApplicationDbContext _dbContext;

        public ProjectUtilizedMaterialController(
            IUnitOfWork unitOfWork,
            IWebHostEnvironment webHostEnvironment,
            ApplicationDbContext dbContext)
        {
            _unitOfWork = unitOfWork;
            _webHostEnvironment = webHostEnvironment;
            _dbContext = dbContext;
        }

        [HttpPost("[action]")]
        [ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
        public async Task<IActionResult> Save([FromBody] PartList material, CancellationToken cancellation)
        {
            if (material.Id == null || material.Id == 0)
            {
                return await Add(material, cancellation);
            }
            var exist = await _unitOfWork.Repository<PartList>().TableNoTracking.AnyAsync(c => c.Id == material.Id);
            if (exist)
            {
                return await Update(material, cancellation);
            }
            return await Add(material, cancellation);
        }

        [HttpPost("[action]")]
        [ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
        public async Task<IActionResult> Add(PartList material, CancellationToken cancellation)
        {
            var entity = await _unitOfWork.Repository<PartList>().SaveAsync(material, cancellation, true);
            return Ok(entity);
        }

        [HttpPost("[action]")]
        [ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
        public async Task<IActionResult> Update(PartList material, CancellationToken cn)
        {
            var entity = await _unitOfWork.Repository<PartList>().UpdateAsync(material, cn, true);
            return Ok(entity);
        }

        [HttpGet("[action]")]
        [ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
        public async Task<IActionResult> Delete(long id, CancellationToken cancellation)
        {
            var model = _unitOfWork.Repository<PartList>().TableNoTracking.FirstOrDefault(c => c.Id == id);
            if (model != null)
                await _unitOfWork.Repository<PartList>().DeleteAsync(model, cancellation, true);
            return Ok();
        }

        [HttpGet("[action]")]
        [ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
        public IActionResult Edit(long? id)
        {
            if (id != null && id != 0)
            {
                var entity = _unitOfWork.Repository<PartList>().TableNoTracking
                    .Include(c => c.PartListGroup)
                    .Include(c => c.Product)
                    .Include(c => c.Part)
                    .FirstOrDefault(c => c.Id == id);
                return View(@"\Views\Panel\Eng\ProjectUtilizedMaterial\Edit.cshtml", entity);
            }
            var newEntity = new PartList();
            return View(@"\Views\Panel\Eng\ProjectUtilizedMaterial\Edit.cshtml", newEntity);
        }

        [HttpGet("[action]")]
        [ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
        public IActionResult New()
        {
            var newEntity = new PartList();
            return View(@"\Views\Panel\Eng\ProjectUtilizedMaterial\Edit.cshtml", newEntity);
        }

        [HttpGet("[action]")]
        [ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
        public IActionResult List()
        {
            return View(@"\Views\Panel\Eng\ProjectUtilizedMaterial\List.cshtml");
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> DownloadPdf(long? dlId, long? partId)
        {
            try
            {
                byte[] pdfBytes;

                if (dlId.HasValue && dlId.Value > 0)
                {
                    pdfBytes = await GetPartListSectionItemsPdf(dlId.Value);
                }
                else if (partId.HasValue && partId.Value > 0)
                {
                    pdfBytes = await GetPartListSectionsPdf(partId.Value);
                }
                else
                {
                    return BadRequest(new { success = false, message = "لطفاً یکی از پارامترهای dlId یا partId را وارد کنید" });
                }

                if (pdfBytes == null || pdfBytes.Length == 0)
                {
                    return NotFound(new { success = false, message = "داده‌ای برای خروجی PDF یافت نشد" });
                }

                string fileName = dlId.HasValue
                    ? $"PartList_DlId_{dlId}.pdf"
                    : $"ProductSections_PartId_{partId}.pdf";

                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"خطا در ایجاد PDF: {ex.Message}" });
            }
        }

        #region Private Method PDF

        private async Task<byte[]> GetPartListSectionItemsPdf(long dlId)
        {
            var data = await _dbContext.Set<PartList>()
                .Where(it => it.DlId == dlId)
                .Include(it => it.Part)
                .Include(it => it.PartListProductSection)
                .OrderBy(it => it.PartListProductSection.Order)
                .ThenBy(it => it.Order)
                .Select(it => new BomPdfDto
                {
                    Id = it.Id,
                    DlId = it.DlId,
                    ProductId = it.ProductId,
                    ProductSectionId = it.PartListProductSectionId,
                    PartId = it.PartId,
                    Qty = it.Qty,
                    Order = it.Order,
                    SectionTitle = it.PartListProductSection.Title,
                    PicturePath = it.PartListProductSection.PicturePath,
                    PartListSectionOrder = it.PartListProductSection.Order,
                    PartCode = it.Part.Code,
                    PartName = it.Part.Name,
                    LatinName = it.Part.LatinTitle
                }).ToListAsync();

            var pdf = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(20);

                    page.Header()
                        .Text($"EngPartListReport - DlId : {dlId}")
                        .FontSize(20)
                        .Bold();

                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(100);
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.ConstantColumn(60);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Text("Image");
                            header.Cell().Text("Part Code");
                            header.Cell().Text("Part Name");
                            header.Cell().Text("Qty");
                        });

                        foreach (var item in data)
                        {
                            table.Cell().Element(cell =>
                            {
                                if (!string.IsNullOrEmpty(item.PicturePath) && System.IO.File.Exists(item.PicturePath))
                                {
                                    var img = System.IO.File.ReadAllBytes(item.PicturePath);
                                    cell.Height(60).Image(img);
                                }
                                else
                                {
                                    cell.Text("-");
                                }
                            });

                            table.Cell().Text(item.PartCode ?? "-");
                            table.Cell().Text(item.PartName ?? "-");
                            table.Cell().Text(item.Qty?.ToString() ?? "-");
                        }
                    });
                });
            });

            return pdf.GeneratePdf();
        }

        private async Task<byte[]> GetPartListSectionsPdf(long partId)
        {
            var data = await _dbContext.Set<PartListProductSection>()
                .AsNoTracking()
                .Where(x => x.PartId == partId)
                .OrderBy(x => x.Order)
                .Select(x => new ProductSectionDto
                {
                    Id = x.Id,
                    Title = x.Title,
                    Order = x.Order,
                    PicturePath = x.PicturePath,
                    ProductCode = x.Part.Code,
                    ProductName = x.Part.Name,
                    ModelTitle = x.Part.Brand,
                    ProductId = x.Part.Id
                })
                .ToListAsync();

            if (!data.Any()) return Array.Empty<byte>();

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Verdana));

                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("Product Sections Report").FontSize(20).SemiBold().FontColor(Colors.Blue.Medium);
                            col.Item().Text($"{data.First().ProductName} ({data.First().ProductCode})").FontSize(12);
                        });

                        row.ConstantItem(100).AlignRight().Text(DateTime.Now.ToString("yyyy/MM/dd")).FontSize(10);
                    });

                    page.Content().PaddingVertical(10).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(80);
                            columns.RelativeColumn();
                            columns.ConstantColumn(100);
                            columns.ConstantColumn(50);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(CellStyle).Text("Image");
                            header.Cell().Element(CellStyle).Text("Section Title");
                            header.Cell().Element(CellStyle).Text("Model");
                            header.Cell().Element(CellStyle).Text("Order");

                            static IContainer CellStyle(IContainer container)
                            {
                                return container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Black);
                            }
                        });

                        foreach (var item in data)
                        {
                            var imgBytes = TryGetImage(item.PicturePath);
                            if (imgBytes != null)
                                table.Cell().PaddingVertical(5).Image(imgBytes).FitArea();
                            else
                                table.Cell().PaddingVertical(5).AlignCenter().AlignMiddle().Text("No Image").FontSize(8).FontColor(Colors.Grey.Medium);

                            table.Cell().Element(RowStyle).Text(item.Title ?? "-");
                            table.Cell().Element(RowStyle).Text(item.ModelTitle ?? "-");
                            table.Cell().Element(RowStyle).AlignCenter().Text(item.Order?.ToString() ?? "-");

                            static IContainer RowStyle(IContainer container)
                            {
                                return container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5).AlignMiddle();
                            }
                        }
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                        x.Span(" of ");
                        x.TotalPages();
                    });
                });
            });

            return document.GeneratePdf();
        }

        private byte[]? TryGetImage(string? path)
        {
            if (string.IsNullOrWhiteSpace(path)) return null;

            try
            {
                if (System.IO.File.Exists(path))
                {
                    return System.IO.File.ReadAllBytes(path);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading image from {path}: {ex.Message}");
            }
            return null;
        }

        #endregion
    }

    #region Dto

    public class BomPdfDto
    {
        public long? Id { get; set; }
        public long? DlId { get; set; }
        public long? ProductId { get; set; }
        public long? ProductSectionId { get; set; }
        public long? PartId { get; set; }
        public long? Qty { get; set; }
        public long? Order { get; set; }
        public string SectionTitle { get; set; }
        public string PicturePath { get; set; }
        public string? PartCode { get; set; }
        public string PartName { get; set; }
        public string LatinName { get; set; }
        public int? PartListSectionOrder { get; set; }
    }

    public class ProductSectionDto
    {
        public long? Id { get; set; }
        public string Title { get; set; }
        public int? Order { get; set; }
        public string PicturePath { get; set; }
        public string ProductCode { get; set; }
        public string ProductName { get; set; }
        public string ModelTitle { get; set; }
        public string PartBookSectionTitle { get; set; }
        public long? ProductId { get; set; }
    }

    #endregion
}