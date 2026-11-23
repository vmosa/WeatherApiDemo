using System.Globalization;
using System.Text.Json.Serialization;

namespace WeatherApiDemo.Server.Weather;

public readonly record struct Coordinates
{
    // Two decimals (~1 km) keeps forecasts accurate while letting nearby requests share cache entries.
    private const int Precision = 2;

    public Coordinates(double latitude, double longitude)
    {
        Latitude = Math.Round(latitude, Precision);
        Longitude = Math.Round(longitude, Precision);
    }

    public double Latitude { get; }

    public double Longitude { get; }

    [JsonIgnore]
    public string LatitudeText => Latitude.ToString("F2", CultureInfo.InvariantCulture);

    [JsonIgnore]
    public string LongitudeText => Longitude.ToString("F2", CultureInfo.InvariantCulture);

    public override string ToString() => $"{LatitudeText},{LongitudeText}";
}
