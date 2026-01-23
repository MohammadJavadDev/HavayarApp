using System.ComponentModel.DataAnnotations;

namespace Entities.App.Sale.Enums
{
	public enum ProductionOrderItemEquipmentTypeEnum
	{
		[Display(Name = "تجهیز اصلی")]
		MainEquipment = 1947,
		[Display(Name = "متعلقات تجهیز")]
		EquipmentAccessories = 1948,
	}
}
