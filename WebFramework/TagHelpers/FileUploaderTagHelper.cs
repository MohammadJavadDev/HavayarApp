using Entities.Base;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System.Collections.Generic;
using System.Text.Encodings.Web;

namespace WebFramework.TagHelpers
{
    [HtmlTargetElement("fileuploader")]
    public class FileUploaderTagHelper : TagHelper
    {
        public required string Bind { get; set; }
        public string? Label { get; set; }
        public long? FileId { get; set; }
        public FileEntity? FileEntity { get; set; }

        // Dropzone settings
        public bool Multiple { get; set; } = false;
        public int MaxFiles { get; set; } = 1;
        public int MaxFileSize { get; set; } = 10; // MB
        public string? AcceptedFileTypes { get; set; } // e.g., "image/*,.pdf,.doc,.docx"
        public bool ShowPreview { get; set; } = true;
        public bool ShowDownload { get; set; } = true;
        public bool ShowDelete { get; set; } = true;
        public required string EntityType { get; set; }
        public required string EntityPropName { get; set; }
          public bool Required { get; set; } = false;
          public long? EntityId { get; set; }

        // Display mode: form, form-item, table
        public string DisplayMode { get; set; } = "form"; // form, form-item, table

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = "div";
            output.AddClass("file-uploader-container", HtmlEncoder.Default);

            var containerClass = DisplayMode switch
            {
                "form-item" => "file-uploader-form-item",
                "table" => "file-uploader-table",
                _ => "file-uploader-form"
            };
            output.AddClass(containerClass, HtmlEncoder.Default);

            var uniqueId = $"fileUploader_{Guid.NewGuid():N}";
            var maxFilesAttr = Multiple ? MaxFiles : 1;
            var acceptedTypes = AcceptedFileTypes ?? "*.*";

            // Determine file ID: prioritize FileId, then FileEntity.Id, then empty
            var resolvedFileId = FileId ?? FileEntity?.Id;
            var fileIdValue = resolvedFileId?.ToString() ?? "";
            var filePathValue = FileEntity?.PhysicalPath ?? "";
            var fileNameValue = FileEntity?.OriginalName ?? "";

            var labelHtml = !string.IsNullOrEmpty(Label)
                ? $@"<label class='form-label'>{Label}</label>"
                : "";

            // Check if file exists: FileId has value OR FileEntity exists with Id
            var hasFile = (FileId.HasValue && FileId.Value > 0) || 
                         (FileEntity != null && FileEntity.Id.HasValue && FileEntity.Id.Value > 0);

            var existingFileHtml = "";
            var dropzoneStyle = "";

            if (hasFile && FileEntity != null && resolvedFileId.HasValue)
            {
                // File exists, show existing file and hide dropzone
                var fileIdForHtml = resolvedFileId.Value;
                var fileExtension = GetFileExtension(fileNameValue);
                var iconClass = GetFileIconClass(fileExtension);
                
                existingFileHtml = $@"
                    <div class='existing-file-item' data-file-id='{fileIdForHtml}'>
                        <div class='file-preview'>
                            <i class='{iconClass}'>
                                <span class='path1'></span>
                                <span class='path2'></span>
                            </i>
                        </div>
                        <div class='file-info'>
                            <span class='file-name'>{HtmlEncoder.Default.Encode(fileNameValue)}</span>
                            <span class='file-size'>{FormatFileSize(FileEntity.Size)}</span>
                        </div>
                        <div class='file-actions'>
                            {(ShowDownload ? $@"<button type='button' class='btn btn-sm btn-icon btn-light btn-download-file' data-file-id='{fileIdForHtml}' title='دانلود'>
                                <i class='ki-duotone ki-arrow-down fs-5'>
                                    <span class='path1'></span>
                                    <span class='path2'></span>
                                </i>
                            </button>" : "")}
                            {(ShowDelete ? $@"<button type='button' class='btn btn-sm btn-icon btn-light btn-delete-file' data-file-id='{fileIdForHtml}' title='حذف'>
                                <i class='ki-duotone ki-trash fs-5'>
                                    <span class='path1'></span>
                                    <span class='path2'></span>
                                </i>
                            </button>" : "")}
                        </div>
                    </div>";
                dropzoneStyle = "display: none;";
            }

            output.Content.SetHtmlContent($@"
                {labelHtml}
                {existingFileHtml}
                <div class='file-uploader-wrapper' id='{uniqueId}' {(hasFile ? "style='display: none;'" : "")}>
                    <div class='dropzone-area' 
                         data-file-uploader='true'
                         data-bindid='{Bind}'
                         data-file-id='{fileIdValue}'
                         data-multiple='{Multiple.ToString().ToLower()}'
                         data-max-files='{maxFilesAttr}'
                         data-max-file-size='{MaxFileSize}'
                         data-accepted-file-types='{acceptedTypes}'
                         data-entity-type='{EntityType ?? ""}'
                         data-entity-id='{EntityId?.ToString() ?? ""}'
                         data-show-preview='{ShowPreview.ToString().ToLower()}'
                         data-show-download='{ShowDownload.ToString().ToLower()}'
                         data-show-delete='{ShowDelete.ToString().ToLower()}'
                         data-entity-prop-name='{EntityPropName}'
                         data-required={Required}
                         style='{dropzoneStyle}'>
                        <div class='dz-message'>
                            
                            <p>فایل را اینجا رها کنید یا کلیک کنید</p>
                            <p class='text-muted small'>حداکثر {MaxFileSize} مگابایت</p>
                        </div>
                    </div>
          
                </div>
                <input type='hidden' data-bind='{Bind}' value='{fileIdValue}' />
            ");
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        /// <summary>
        /// Get file extension from file name
        /// </summary>
        /// <param name="fileName">The file name</param>
        /// <returns>The file extension (without dot) or empty string</returns>
        private string GetFileExtension(string? fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return string.Empty;

            // Trim whitespace
            fileName = fileName.Trim();
            if (string.IsNullOrEmpty(fileName))
                return string.Empty;

            // Handle hidden files (starting with dot) - return empty for .hidden files
            if (fileName.StartsWith('.') && fileName.IndexOf('.', 1) == -1)
                return string.Empty;

            // Split by dot and get last part
            var parts = fileName.Split('.');
            
            // If no extension (only one part or last part is empty), return empty
            if (parts.Length < 2 || string.IsNullOrEmpty(parts[parts.Length - 1]))
                return string.Empty;

            // Return last part (extension) in lowercase
            return parts[parts.Length - 1].ToLowerInvariant();
        }

        /// <summary>
        /// Get file icon class based on file extension
        /// </summary>
        /// <param name="extension">The file extension (without dot)</param>
        /// <returns>The CSS class for the file icon</returns>
        private string GetFileIconClass(string extension)
        {
            if (string.IsNullOrWhiteSpace(extension))
                return "ki-duotone ki-file fs-2x text-primary";

            var iconMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "pdf", "fas fa-file-pdf fs-2x text-danger" },
                { "doc", "fas fa-file-word fs-2x text-primary" },
                { "docx", "fas fa-file-word fs-2x text-primary" },
                { "xls", "fas fa-file-excel fs-2x text-success" },
                { "xlsx", "fas fa-file-excel fs-2x text-success" },
                { "ppt", "fas fa-file-prescription fs-2x text-warning" },
                { "pptx", "fas fa-file-prescription fs-2x text-warning" },
                { "zip", "fas fa-file-archive fs-2x text-info" },
                { "rar", "fas fa-file-archive fs-2x text-info" },
                { "7z", "fas fa-file-archive fs-2x text-info" },
                { "jpg", "ki-outline ki-picture fs-2x text-primary" },
                { "jpeg", "ki-outline ki-picture fs-2x text-primary" },
                { "png", "ki-outline ki-picture fs-2x text-primary" },
                { "gif", "ki-outline ki-picture fs-2x text-primary" },
                { "txt", "fas fa-file fs-2x text-muted" },
                { "csv", "fas fa-file-csv fs-2x text-info" },
                { "xml", "fas fa-file fs-2x text-info" },
                { "html", "fas fa-file fs-2x text-warning" },
                { "mp3", "fas fa-file fs-2x text-primary" },
                { "mp4", "fas fa-file fs-2x text-primary" },
                { "avi", "fas fa-file fs-2x text-primary" }
            };

            return iconMap.TryGetValue(extension, out var iconClass) 
                ? iconClass 
                : "ki-duotone ki-file fs-2x text-primary";
        }
    }
}

