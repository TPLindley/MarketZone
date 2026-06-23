namespace mzWheeler.Models;

/// <summary>
/// OBD-II vehicle data
/// </summary>
public class VehicleData
{
    public double Speed { get; set; }           // km/h
    public double Rpm { get; set; }             // RPM
    public double EngineLoad { get; set; }      // %
    public double Throttle { get; set; }        // %
    public double CoolantTemp { get; set; }     // °C
    public double FuelLevel { get; set; }       // %
    public double BatteryVoltage { get; set; }  // V
    public double FuelConsumption { get; set; } // L/100km
    public DateTime Timestamp { get; set; } = DateTime.Now;
}
