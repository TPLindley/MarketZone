using mzWheeler.Models;

namespace mzWheeler.Services;

/// <summary>
/// Service for accessing device tilt/orientation sensors
/// </summary>
public class TiltService
{
    private bool _isMonitoring;

    public event EventHandler<TiltData>? TiltUpdated;
    public event EventHandler<string>? StatusChanged;

    /// <summary>
    /// Start monitoring accelerometer and gyroscope
    /// </summary>
    public void StartMonitoring()
    {
        if (_isMonitoring)
            return;

        try
        {
            // Start accelerometer
            if (Accelerometer.Default.IsSupported)
            {
                Accelerometer.Default.ReadingChanged += OnAccelerometerReadingChanged;
                Accelerometer.Default.Start(SensorSpeed.UI);
            }

            // Start gyroscope
            if (Gyroscope.Default.IsSupported)
            {
                Gyroscope.Default.ReadingChanged += OnGyroscopeReadingChanged;
                Gyroscope.Default.Start(SensorSpeed.UI);
            }

            _isMonitoring = true;
            StatusChanged?.Invoke(this, "Monitoring tilt sensors");
        }
        catch (Exception ex)
        {
            StatusChanged?.Invoke(this, $"Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Stop monitoring sensors
    /// </summary>
    public void StopMonitoring()
    {
        if (!_isMonitoring)
            return;

        try
        {
            if (Accelerometer.Default.IsMonitoring)
            {
                Accelerometer.Default.ReadingChanged -= OnAccelerometerReadingChanged;
                Accelerometer.Default.Stop();
            }

            if (Gyroscope.Default.IsMonitoring)
            {
                Gyroscope.Default.ReadingChanged -= OnGyroscopeReadingChanged;
                Gyroscope.Default.Stop();
            }

            _isMonitoring = false;
            StatusChanged?.Invoke(this, "Monitoring stopped");
        }
        catch (Exception ex)
        {
            StatusChanged?.Invoke(this, $"Error: {ex.Message}");
        }
    }

    private TiltData _currentTiltData = new TiltData();

    private void OnAccelerometerReadingChanged(object? sender, AccelerometerChangedEventArgs e)
    {
        var reading = e.Reading;

        _currentTiltData.AccelerationX = reading.Acceleration.X;
        _currentTiltData.AccelerationY = reading.Acceleration.Y;
        _currentTiltData.AccelerationZ = reading.Acceleration.Z;

        // Calculate pitch and roll from accelerometer
        _currentTiltData.Pitch = CalculatePitch(reading.Acceleration.X, reading.Acceleration.Y, reading.Acceleration.Z);
        _currentTiltData.Roll = CalculateRoll(reading.Acceleration.X, reading.Acceleration.Y, reading.Acceleration.Z);

        _currentTiltData.Timestamp = DateTime.Now;

        TiltUpdated?.Invoke(this, _currentTiltData);
    }

    private void OnGyroscopeReadingChanged(object? sender, GyroscopeChangedEventArgs e)
    {
        var reading = e.Reading;

        _currentTiltData.AngularVelocityX = reading.AngularVelocity.X;
        _currentTiltData.AngularVelocityY = reading.AngularVelocity.Y;
        _currentTiltData.AngularVelocityZ = reading.AngularVelocity.Z;

        // Yaw can be calculated from gyroscope (requires integration over time)
        // For simplicity, we'll use the Z-axis angular velocity
        _currentTiltData.Yaw += reading.AngularVelocity.Z * (180 / Math.PI) * 0.1; // Approximate integration

        _currentTiltData.Timestamp = DateTime.Now;

        TiltUpdated?.Invoke(this, _currentTiltData);
    }

    /// <summary>
    /// Calculate pitch angle from accelerometer data
    /// </summary>
    private double CalculatePitch(double x, double y, double z)
    {
        return Math.Atan2(x, Math.Sqrt(y * y + z * z)) * (180.0 / Math.PI);
    }

    /// <summary>
    /// Calculate roll angle from accelerometer data
    /// </summary>
    private double CalculateRoll(double x, double y, double z)
    {
        return Math.Atan2(y, Math.Sqrt(x * x + z * z)) * (180.0 / Math.PI);
    }

    /// <summary>
    /// Check if tilt sensors are supported
    /// </summary>
    public bool IsSupported()
    {
        return Accelerometer.Default.IsSupported || Gyroscope.Default.IsSupported;
    }
}
