using Data.Contracts;
using Entities.App.Sale;
using Microsoft.EntityFrameworkCore;

namespace WebApp.Services
{

	namespace WebApp.Controllers.Dynamic
	{
		public class SaleMissionService
		{
			private readonly IUnitOfWork _unitOfWork;

			public SaleMissionService(IUnitOfWork unitOfWork)
			{
				_unitOfWork = unitOfWork;
			}

			public void CalculateTotalCost(SaleMission mission)
			{
				if (mission == null) return;

				decimal breakfastTotal = (mission.BreakfastPrice ?? 0) * (mission.BreakfastQuantity ?? 0);
				decimal lunchTotal = (mission.LunchPrice ?? 0) * (mission.LunchQuantity ?? 0);
				decimal dinnerTotal = (mission.DinnerPrice ?? 0) * (mission.DinnerQuantity ?? 0);
				decimal nightShiftTotal = (mission.NightShiftAllowance ?? 0) * (mission.NightShiftCoefficient ?? 0);

				double overtimeHours = 0;
				if (!string.IsNullOrEmpty(mission.WorkOvertimeHour))
				{
					double.TryParse(mission.WorkOvertimeHour.Replace('/', '.'), out overtimeHours);
				}
				long overtimeTotal = (long)((mission.WorkOvertimeFee ?? 0) * overtimeHours);

				long missionRightTotal = (mission.MissionRight ?? 0) * (mission.MissionRightFactor ?? 0);
				long holidayRightTotal = (mission.MissionHolidayRight ?? 0) * (mission.MissionHolidayRightFactor ?? 0);

				long dailySalary = mission.DailySalary ?? 0;
				long transportationPersonal = mission.TransportationPersonal ?? 0;
				long transportationDriving = mission.TransportationDriving ?? 0;
				long otherCost = mission.OtherCost ?? 0;

				decimal totalDecimal = breakfastTotal + lunchTotal + dinnerTotal + nightShiftTotal;
				long totalLong = (long)totalDecimal + overtimeTotal + missionRightTotal + holidayRightTotal +
							dailySalary + transportationPersonal + transportationDriving + otherCost;

				mission.TotalCost = totalLong;
			}

			public (int breakfastCount, int lunchCount, int dinnerCount, int wentExtraWorkHours, int finalMissionDays, int nightShiftCoefficient, int workOvertimeHour, int mission_Right_Factor)
			    CalculateMissionMeals(DateTime fromDate, DateTime toDate, int missionDays)
			{
				var departureHour = 8;
				var returnHour = 17;

				var breakfastCount = 0;
				var lunchCount = 0;
				var dinnerCount = 0;
				var wentExtraWorkHours = 0;
				var finalMissionDays = missionDays;
				var nightShiftCoefficient = missionDays;
				var mission_Right_Factor = missionDays;
				var workOvertimeHour = 0;

				if (missionDays <= 1)
				{
					if (departureHour <= 9)
						breakfastCount += 1;

					if (departureHour <= 8)
						wentExtraWorkHours = 8 - departureHour;

					if (departureHour <= 14 || returnHour >= 12)
						lunchCount += 1;

					if (returnHour >= 20 && toDate.Minute >= 30)
						dinnerCount += 1;

					if (departureHour > 12)
						finalMissionDays += 1;
				}
				else if (missionDays == 2)
				{
					if (departureHour <= 9)
					{
						breakfastCount += 1;
						lunchCount += 1;
						dinnerCount += 1;
					}
					else
					{
						if (departureHour <= 14)
							lunchCount += 1;

						if (departureHour <= 20)
							dinnerCount += 1;
					}

					if (departureHour <= 8)
						wentExtraWorkHours = 8 - departureHour;
					breakfastCount += 1;

					if (returnHour >= 12)
						lunchCount += 1;

					if (returnHour >= 20)
						dinnerCount += 1;

					if (departureHour > 12)
						finalMissionDays += 1;
				}
				else
				{
					var eatCount = missionDays - 2;
					breakfastCount += eatCount + 1;
					lunchCount += eatCount;
					dinnerCount += eatCount;

					if (departureHour <= 9)
					{
						breakfastCount += 1;
						lunchCount += 1;
						dinnerCount += 1;
					}
					else
					{
						if (departureHour <= 14)
							lunchCount += 1;

						if (departureHour <= 20)
							dinnerCount += 1;
					}

					if (departureHour <= 8)
						wentExtraWorkHours = 8 - departureHour;

					if (returnHour >= 12)
						lunchCount += 1;

					if (returnHour >= 20)
						dinnerCount += 1;

					if (departureHour > 12)
						finalMissionDays += 1;
				}

				return (breakfastCount, lunchCount, dinnerCount, wentExtraWorkHours, finalMissionDays, nightShiftCoefficient, workOvertimeHour, mission_Right_Factor);
			}

			public async Task<MissionSalaryPersonnel> GetMissionSalaryAsync(long? personelId)
			{
				return await _unitOfWork.Repository<MissionSalaryPersonnel>()
				    .TableNoTracking
				    .Where(it => it.PersonelId == personelId)
				    .FirstOrDefaultAsync();
			}
		}

	}
}

public static class DateTimeHelper
{
	public static double DurationInMinutes(DateTime fromDate, DateTime toDate)
	{
		return (toDate - fromDate).TotalMinutes;
	}
}