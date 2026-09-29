using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace TreatmentService.Infrastructure.Persistence.Converters;

internal sealed class TimeOnlyConverter : ValueConverter<TimeOnly, TimeSpan>
{
    public TimeOnlyConverter() : base(
        timeOnly => timeOnly.ToTimeSpan(),
        timeSpan => TimeOnly.FromTimeSpan(timeSpan))
    {
    }
}
