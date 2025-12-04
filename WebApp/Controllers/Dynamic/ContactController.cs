using Common.Attributes;
using Common.Auth.Enums;
using Common.System;
using Common.Utilities;
using Data.Contracts;
using Data.SystemAuth;
using Entities.App;
using Entities.Base.DataTable;
 
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Services.NotificationServices;
using Stimulsoft.System.Windows.Forms;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.Dynamic
{
    [Route("Panel/[controller]")]
    [ApiController]
    [ApiResultFilter]
    [ControllerInfo("مخاطب",typeof(Contact))]
    public class ContactController(IUnitOfWork unitOfWork  
         , IPropertyIdentityService identityService 
         , INotificationService notificationService,
         ISdk sdk
         ) : BaseController
	{
        [HttpPost("[action]")]
        [ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
        public async Task<IActionResult> Save(Contact contact , CancellationToken cn)
        {

			if(contact.ContactType == ContactType.Real)
            {
                
                if(!contact.NationalCode.HasValue(true))
                {
                    throw new Exception("ثبت کد ملی برای مخاطب حقیقی الزامی است.");
                }

                if (unitOfWork.Repository<Contact>().TableNoTracking
					.Any(c => c.NationalCode == contact.NationalCode
					&& c.Id != contact.Id))
                {
                    throw new Exception("مخاطب با این کد ملی قبلا در سیستم ثبت شده است.");
                }
			}
            else
            {
				if (!contact.NationalId.HasValue(true))
				{
					throw new Exception("ثبت شناسه ملی برای مخاطب حقوقی الزامی است.");
				}
				if (unitOfWork.Repository<Contact>().TableNoTracking
				  .Any(c => c.NationalId == contact.NationalId
				  && c.Id != contact.Id))
				{
					throw new Exception("مخاطب با این کد ملی قبلا در سیستم ثبت شده است.");
				}
			}

		 

			// Save logic here
			if (contact.Id == null || contact.Id == 0)
            {
                 return await  Add(contact, cn);
            }
             var exist = await unitOfWork.Repository<Contact>().TableNoTracking
                    .AnyAsync(c => c.Id == contact.Id);
           if(exist)
           {
                return await  Update(contact, cn);
           }
              return await  Add(contact, cn);
        }

        [HttpPost("[action]")]
        [ActionDisplayName("درج", ActionAccessType.Api,ActionAccessItemType.Create)]
        public async Task<IActionResult> Add(Contact contact , CancellationToken cn)
        {
            // Add logic here
           var entity = await unitOfWork.Repository<Contact>().SaveAsync(contact ,cn,true);
            return Ok(entity);
        }

        [HttpPost("[action]")]
        [ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
        public async Task<IActionResult> Update(Contact contact , CancellationToken cn)
        {
            // Update logic here
           var entity = await unitOfWork.Repository<Contact>().UpdateAsync(contact, cn, true);
            return Ok(entity);
        }

        [HttpGet("[action]")]
        [ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
            public  async Task<IActionResult> Delete(long id, CancellationToken cn)
        {
            // Delete logic here
           var model = unitOfWork.Repository<Contact>().TableNoTracking.FirstOrDefault(c => c.Id == id);
            if (model != null)
              await unitOfWork.Repository<Contact>().DeleteAsync(model ,cn , true);
            return Ok();
        }

        [HttpGet("[action]")]
        [ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
        public IActionResult Edit(long? id)
        {
            // Get logic here
            if (id != null && id != 0)
            {
             var entity =   unitOfWork.Repository<Contact>().TableNoTracking.FirstOrDefault(c => c.Id == id);
                return View(@"\Views\Panel\Contact\Edit.cshtml", entity);
            }
            var newEntity = new Contact(); 
               
            return View(@"\Views\Panel\Contact\Edit.cshtml",newEntity);
        }
        [HttpGet("[action]")]
        [ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
        public IActionResult New()
        {
              var newEntity = new Contact();
 

			return View(@"\Views\Panel\Contact\Edit.cshtml",newEntity);
        }
        [HttpGet("[action]")]
        [ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
        public IActionResult List()
        {
              return View(@"\Views\Panel\Contact\List.cshtml");
        }
        [ActionDisplayName("دریافت اطلاعات", ActionAccessType.Api, ActionAccessItemType.FetchData)]
        [HttpPost("[action]")]
         public async Task<IActionResult> FetchData(DataTableRequest request , CancellationToken cn)
        {
              return Ok(await unitOfWork.Repository<Contact>().FetchDataAsync(request ,cn));
        }

    }
}
