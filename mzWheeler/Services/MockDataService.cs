using mzWheeler.Models;

namespace mzWheeler.Services;

/// <summary>
/// Provides simulated sensor and OBD data for testing without hardware
/// </summary>
public class MockDataService
{
    private readonly Random _random = new();
    private double _simulatedSpeed = 0;
    private double _simulatedRpm = 800;
    private double _simulatedThrottle = 0;
    private bool _isAccelerating = true;
    private double _simulatedLatitude = 33.4484; // Phoenix, AZ
    private double _simulatedLongitude = -112.0740;
    private double _simulatedCourse = 0;

    /// <summary>
    /// Generate realistic vehicle data that changes over time
    /// </summary>
    public VehicleData GenerateMockVehicleData()
    {
        // Simulate acceleration/deceleration cycle
        if (_isAccelerating)
        {
            _simulatedSpeed += _random.NextDouble() * 2;
            _simulatedRpm += _random.NextDouble() * 150;
            _simulatedThrottle = Math.Min(100, _simulatedThrottle + _random.NextDouble() * 5);

            if (_simulatedSpeed > 100)
            {
                _isAccelerating = false;
            }
        }
        else
        {
            _simulatedSpeed -= _random.NextDouble() * 3;
            _simulatedRpm -= _random.NextDouble() * 200;
            _simulatedThrottle = Math.Max(0, _simulatedThrottle - _random.NextDouble() * 8);

            if (_simulatedSpeed < 20)
            {
                _isAccelerating = true;
            }
        }

        // Keep values in realistic ranges
        _simulatedSpeed = Math.Clamp(_simulatedSpeed, 0, 180);
        _simulatedRpm = Math.Clamp(_simulatedRpm, 800, 6500);

        return new VehicleData
        {
            Speed = _simulatedSpeed,
            Rpm = _simulatedRpm,
            EngineLoad = Math.Min(100, (_simulatedRpm / 6500.0) * 100 + _random.NextDouble() * 10),
            Throttle = _simulatedThrottle,
            CoolantTemp = 85 + _random.NextDouble() * 10, // Normal operating temp
            FuelLevel = 45 + _random.NextDouble() * 5, // Around half tank
            BatteryVoltage = 13.8 + _random.NextDouble() * 0.4, // Normal charging voltage
            FuelConsumption = (_simulatedSpeed > 0) ? (8 + _random.NextDouble() * 4) : 0,
            Timestamp = DateTime.Now
        };
    }

    /// <summary>
    /// Generate GPS data that simulates movement
    /// </summary>
    public LocationData GenerateMockLocationData()
    {
        // Simulate movement by slightly changing position
        _simulatedLatitude += (_random.NextDouble() - 0.5) * 0.0001; // ~11 meters
        _simulatedLongitude += (_random.NextDouble() - 0.5) * 0.0001;
        _simulatedCourse += (_random.NextDouble() - 0.5) * 10; // Slight course changes
        _simulatedCourse = (_simulatedCourse + 360) % 360; // Keep 0-360

        var speed = 20 + _random.NextDouble() * 30; // 20-50 km/h simulated

        return new LocationData
        {
            Latitude = _simulatedLatitude,
            Longitude = _simulatedLongitude,
            Altitude = 340 + _random.NextDouble() * 2, // Phoenix elevation ~340m
            Speed = speed / 3.6, // Convert km/h to m/s
            Course = _simulatedCourse,
            Accuracy = 5 + _random.NextDouble() * 5, // 5-10m accuracy
            Timestamp = DateTime.Now
        };
    }

    /// <summary>
    /// Generate tilt data simulating device movement
    /// </summary>
    public TiltData GenerateMockTiltData()
    {
        var time = DateTime.Now.TimeOfDay.TotalSeconds;

        // Use sine waves for smooth, realistic tilt simulation
        var pitch = Math.Sin(time * 0.3) * 15; // ±15° pitch
        var roll = Math.Sin(time * 0.5) * 10; // ±10° roll
        var yaw = (time * 5) % 360; // Slowly rotating yaw

        return new TiltData
        {
            AccelerationX = Math.Sin(time * 0.3) * 0.3,
            AccelerationY = Math.Sin(time * 0.5) * 0.2,
            AccelerationZ = 9.8 + Math.Sin(time * 0.2) * 0.5, // Gravity + small variation
            AngularVelocityX = Math.Sin(time * 0.4) * 0.5,
            AngularVelocityY = Math.Sin(time * 0.6) * 0.3,
            AngularVelocityZ = Math.Sin(time * 0.3) * 0.4,
            Pitch = pitch,
            Roll = roll,
            Yaw = yaw,
            Timestamp = DateTime.Now
        };
    }

    /// <summary>
    /// Reset simulation to starting values
    /// </summary>
    public void Reset()
    {
        _simulatedSpeed = 0;
        _simulatedRpm = 800;
        _simulatedThrottle = 0;
        _isAccelerating = true;
        _simulatedLatitude = 33.4484;
        _simulatedLongitude = -112.0740;
        _simulatedCourse = 0;
    }
}
