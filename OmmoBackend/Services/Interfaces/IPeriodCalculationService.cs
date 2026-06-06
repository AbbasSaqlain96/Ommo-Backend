using OmmoBackend.Helpers.Enums;
using OmmoBackend.Models;

namespace OmmoBackend.Services.Interfaces
{
    public interface IPeriodCalculationService
    {
        DateTimeOffset GetPeriodEnd(PackagePlan plan, DateTimeOffset from);
    }

    public class PeriodCalculationService : IPeriodCalculationService
    {
        public DateTimeOffset GetPeriodEnd(PackagePlan plan, DateTimeOffset from)
        {
            return plan.interval == PlanInterval.annual
                ? from.AddYears(1)
                : from.AddMonths(1);
        }
    }
}
