using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Common.Attributes;
using Common.Utilities;
using Data.Contracts;
using Entities.Base;

namespace Data.Repositories
{
    public class PropertyIdentityService(IUnitOfWork unitOfWork) :IPropertyIdentityService
    {
        public long GenerateNewValueIdentity(string propertyIdentity, string typeName)
        {
          var value=  unitOfWork.Repository<PropertyIdentity>()
                .Table
                .FirstOrDefault(c => c.TypeName == typeName
                                     && c.PropertyName == propertyIdentity);
            long returnNumber = 0;
          if (value == null)
          {
              var entityType = typeof(BaseEntity).Assembly.GetTypes().FirstOrDefault(t => t.Name.ToLower() == typeName.ToLower());
              if (entityType == null)
              {
                  entityType = Assembly.GetExecutingAssembly().GetTypes().FirstOrDefault(t => t.Name.ToLower() == typeName.ToLower());
                  if (entityType == null)
                      throw new Exception("خطا در ایجاد شمارنده خودکار نوع یافت نشد.");
              }

              var prop = entityType.GetProperty(propertyIdentity);

                var displayNameAttr = prop.GetCustomAttribute<DisplayInfoAttribute>();
                long step = 1;
                long start = 1;
                if (displayNameAttr != null)
                {
                    step = displayNameAttr?.Step ?? 1;
                    start = displayNameAttr?.Start ?? 1;
                }
                value =   unitOfWork.Repository<PropertyIdentity>()
                    .Add(new PropertyIdentity()
                    {
                        PropertyName = propertyIdentity,
                        TypeName = typeName,
                        Step = step,
                        Number = start,
                        Prefix = 10
                    });

                var str = value.Number.ToString();
                str = value.Prefix.ToString() + str;

                returnNumber = str.ToLong();
            }
          else
          {
              value.Number += value.Step;
                var str = value.Number.ToString();
                str = value.Prefix.ToString() + str;

                returnNumber = str.ToLong();

              unitOfWork.Repository<PropertyIdentity>()
                  .Update(value);
          }
  

          return returnNumber;

        }
        public List<long> GenerateNewValueIdentityRange(string propertyIdentity, string typeName , long skip)
        {
            var value = unitOfWork.Repository<PropertyIdentity>()
                .Table
                .FirstOrDefault(c => c.TypeName == typeName
                                     && c.PropertyName == propertyIdentity);
            var valueRetrun = 1;
            if (value == null)
            {
                var entityType = typeof(BaseEntity).Assembly.GetTypes().FirstOrDefault(t => t.Name.ToLower() == typeName.ToLower());
                if (entityType == null)
                {
                    entityType = Assembly.GetExecutingAssembly().GetTypes().FirstOrDefault(t => t.Name.ToLower() == typeName.ToLower());
                    if (entityType == null)
                        throw new Exception("خطا در ایجاد شمارنده خودکار نوع یافت نشد.");
                }

                var prop = entityType.GetProperty(propertyIdentity);

                var displayNameAttr = prop.GetCustomAttribute<DisplayInfoAttribute>();
                long step = 1;
                long start = 1;
                if (displayNameAttr != null)
                {
                    step = displayNameAttr?.Step ?? 1;
                    start = displayNameAttr?.Start ?? 1;
                }
                value =  new PropertyIdentity(){
                    PropertyName = propertyIdentity,
                    TypeName = typeName,
                    Step = step,
                    Number = start ,
                    Prefix = 0
                };
            }
            

            var generatedValues = new List<long>();
          

            for (int i = 0; i < skip; i++)
            {
                long returnNumber = 0;

                value.Number += value.Step;
                var str = value.Number.ToString();
                str = value.Prefix.ToString() + str;

                returnNumber = str.ToLong();

                generatedValues.Add(returnNumber);
                
            }

            unitOfWork.Repository<PropertyIdentity>()
                .Save(value);


            return generatedValues;

        }
    }
}
