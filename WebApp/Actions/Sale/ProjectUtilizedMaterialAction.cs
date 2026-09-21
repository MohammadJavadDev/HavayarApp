using Data.Contracts;
using Data.Contracts.Actions;
using Entities.App.Sale;

namespace WebApp.Actions.Sale
{
	/// <summary>
	/// معادل HTS ProjectUtilizedMaterialController.DoOperation:
	/// پس از ویرایش کالای جایگزین / مقدار / توضیحات، یک ردیف Sale_ProjectUtilizedMaterialChanges نوشته می‌شود.
	/// </summary>
	public class ProjectUtilizedMaterialAction(IUnitOfWork unitOfWork)
	{
		[EntityAction(typeof(ProjectUtilizedMaterial), EntityActionTrigger.AfterUpdate,
			"LogProjectUtilizedMaterialChange", "ثبت سابقه تغییر اقلام مصرفی پروژه", Priority = 1)]
		public async Task LogChange(ProjectUtilizedMaterial entity, CancellationToken ct)
		{
			if (entity.Id is null or 0)
				return;

			await unitOfWork.Repository<ProjectUtilizedMaterialChange>().SaveAsync(new ProjectUtilizedMaterialChange
			{
				ProjectUtilizedMaterialId = entity.Id,
				Comment = entity.Comment,
				HtsId = 0
			}, ct, true);
		}
	}
}
