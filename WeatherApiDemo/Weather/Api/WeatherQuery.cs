using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace WeatherApiDemo.Server.Weather.Api;

public sealed class WeatherQuery : IValidatableObject
{
    private const string UnsupportedSortingMessage = "Sorting is not available for these parameters.";
    private const string UnsupportedCategoryMessage = "Filtering by category is not available for these parameters.";

    [FromQuery(Name = "latitude")]
    [Description("Latitude in decimal degrees, -90 to 90.")]
    [Range(-90d, 90d)]
    public double? Latitude { get; init; }

    [FromQuery(Name = "longitude")]
    [Description("Longitude in decimal degrees, -180 to 180.")]
    [Range(-180d, 180d)]
    public double? Longitude { get; init; }

    [FromQuery(Name = "from")]
    [Description("Only readings at or after this time (ISO 8601).")]
    public DateTimeOffset? From { get; init; }

    [FromQuery(Name = "to")]
    [Description("Only readings at or before this time (ISO 8601).")]
    public DateTimeOffset? To { get; init; }

    [FromQuery(Name = "category")]
    [Description("Only readings in these weather categories. Repeatable.")]
    public QueryEnum<WeatherCategory>[]? Category { get; init; }

    [FromQuery(Name = "source")]
    [Description("Only query these providers. Repeatable.")]
    public string[]? Source { get; init; }

    [FromQuery(Name = "sortBy")]
    [Description("Sort field, Date by default. Relevance puts readings nearest to 'from', or now, first.")]
    public QueryEnum<SortField>? SortBy { get; init; }

    [FromQuery(Name = "sortOrder")]
    [Description("Sort direction, Ascending by default.")]
    public QueryEnum<SortOrder>? SortOrder { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (From > To)
        {
            yield return new ValidationResult("'from' must not be later than 'to'.", ["from", "to"]);
        }

        if (Category?.Any(category => !category.IsDefined) == true)
        {
            yield return new ValidationResult(UnsupportedCategoryMessage, ["category"]);
        }

        if (SortBy is { IsDefined: false })
        {
            yield return new ValidationResult(UnsupportedSortingMessage, ["sortBy"]);
        }

        if (SortOrder is { IsDefined: false })
        {
            yield return new ValidationResult(UnsupportedSortingMessage, ["sortOrder"]);
        }
    }
}

public enum SortField
{
    Date,
    Relevance,
    Temperature,
    Category,
    Source
}

public enum SortOrder
{
    Ascending,
    Descending
}
