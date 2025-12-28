using WebApp.ViewModels.FormBuilder;

namespace WebApp.Services
{
    /// <summary>
    /// سرویس خواندن اطلاعات Schema دیتابیس برای فرم‌ساز
    /// </summary>
    public interface IDatabaseSchemaService
    {
        /// <summary>
        /// دریافت لیست Connection String های موجود از تنظیمات
        /// </summary>
        /// <returns>لیست نام و مقدار Connection String ها</returns>
        Task<List<ConnectionStringInfo>> GetConnectionStringsAsync();

        /// <summary>
        /// تست اتصال به دیتابیس
        /// </summary>
        /// <param name="connectionString">رشته اتصال به دیتابیس</param>
        /// <returns>نتیجه تست اتصال</returns>
        Task<ConnectionTestResult> TestConnectionAsync(string connectionString);

        /// <summary>
        /// دریافت لیست جداول دیتابیس
        /// </summary>
        /// <param name="connectionString">رشته اتصال به دیتابیس</param>
        /// <returns>لیست جداول با اطلاعات Schema و تعداد رکورد</returns>
        Task<List<DatabaseTableInfo>> GetTablesAsync(string connectionString);

        /// <summary>
        /// دریافت ستون‌های یک جدول با جزئیات کامل
        /// </summary>
        /// <param name="connectionString">رشته اتصال به دیتابیس</param>
        /// <param name="schema">نام Schema جدول</param>
        /// <param name="tableName">نام جدول</param>
        /// <returns>لیست ستون‌ها با تمام جزئیات و Mapping خودکار</returns>
        Task<List<TableColumnInfo>> GetTableColumnsAsync(string connectionString, string schema, string tableName);
    }
}



