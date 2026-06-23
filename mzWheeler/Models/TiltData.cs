namespace mzWheeler.Models;

/// <summary>
/// Device tilt/orientation data from accelerometer and gyroscope
/// </summary>
public class TiltData
{
    // Accelerometer data (g-force)
    public double AccelerationX { get; set; }
    public double AccelerationY { get; set; }
    public double AccelerationZ { get; set; }

    // Gyroscope data (rad/s)
    public double AngularVelocityX { get; set; }
    public double AngularVelocityY { get; set; }
    public double AngularVelocityZ { get; set; }

    // Calculated tilt angles (degrees)
    public double Pitch { get; set; }  // Forward/backward tilt
    public double Roll { get; set; }   // Left/right tilt
    public double Yaw { get; set; }    // Rotation around vertical axis

    public DateTime Timestamp { get; set; } = DateTime.Now;
}
