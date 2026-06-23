namespace mzWheeler.Models;

/// <summary>
/// GPS and location data
/// </summary>
public class LocationData
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double Altitude { get; set; }        // meters
    public double Speed { get; set; }           // m/s
    public double Course { get; set; }          // degrees (heading)
    public double Accuracy { get; set; }        // meters
    public DateTime Timestamp { get; set; } = DateTime.Now;
}
