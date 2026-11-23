namespace WeatherApiDemo.Server.Weather.Api;

/// <summary>An enum bound from the query string by name, ignoring case.</summary>
public readonly record struct QueryEnum<TEnum> where TEnum : struct, Enum
{
    private QueryEnum(TEnum value, bool isDefined)
    {
        Value = value;
        IsDefined = isDefined;
    }

    public TEnum Value { get; }

    /// <summary>False when the query value names no member; validation reports it.</summary>
    public bool IsDefined { get; }

    public static implicit operator QueryEnum<TEnum>(TEnum value) => new(value, isDefined: true);

    // Minimal APIs bind plain enums case-sensitively, accept numbers and flag combinations, and answer
    // unknown values with a framework message. Binding always succeeds here so validation owns the message.
    public static bool TryParse(string? value, out QueryEnum<TEnum> result)
    {
        var name = Enum.GetNames<TEnum>()
            .FirstOrDefault(name => name.Equals(value?.Trim(), StringComparison.OrdinalIgnoreCase));

        result = name is null ? default : new QueryEnum<TEnum>(Enum.Parse<TEnum>(name), isDefined: true);
        return true;
    }

    public override string ToString() => IsDefined ? Value.ToString() : string.Empty;
}
