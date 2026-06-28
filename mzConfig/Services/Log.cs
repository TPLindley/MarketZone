using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace mzConfigure.Services;

/// <summary>
/// Cross-platform logging service that wraps Debug.WriteLine with structured log levels.
/// Logs appear in the Visual Studio Output window (Debug pane) and platform-specific debug consoles.
/// </summary>
public static class Log
{
    /// <summary>
    /// Enables or disables all logging. Default is true.
    /// </summary>
    public static bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Minimum log level to output. Default is Debug (all messages).
    /// </summary>
    public static LogLevel MinimumLevel { get; set; } = LogLevel.Debug;

    /// <summary>
    /// Log levels in order of severity
    /// </summary>
    public enum LogLevel
    {
        Debug = 0,
        Info = 1,
        Warning = 2,
        Error = 3
    }

    /// <summary>
    /// Logs a debug message. Use for detailed diagnostic information.
    /// </summary>
    /// <param name="message">The message to log</param>
    /// <param name="memberName">Auto-populated with the calling member name</param>
    /// <param name="filePath">Auto-populated with the calling file path</param>
    /// <param name="lineNumber">Auto-populated with the calling line number</param>
    public static void Debug(
        string message,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        Write(LogLevel.Debug, message, memberName, filePath, lineNumber);
    }

    /// <summary>
    /// Logs an informational message. Use for general operations and flow tracking.
    /// </summary>
    /// <param name="message">The message to log</param>
    /// <param name="memberName">Auto-populated with the calling member name</param>
    /// <param name="filePath">Auto-populated with the calling file path</param>
    /// <param name="lineNumber">Auto-populated with the calling line number</param>
    public static void Info(
        string message,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        Write(LogLevel.Info, message, memberName, filePath, lineNumber);
    }

    /// <summary>
    /// Logs a warning message. Use for recoverable issues or unexpected conditions.
    /// </summary>
    /// <param name="message">The message to log</param>
    /// <param name="memberName">Auto-populated with the calling member name</param>
    /// <param name="filePath">Auto-populated with the calling file path</param>
    /// <param name="lineNumber">Auto-populated with the calling line number</param>
    public static void Warning(
        string message,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        Write(LogLevel.Warning, message, memberName, filePath, lineNumber);
    }

    /// <summary>
    /// Logs an error message. Use for failures and exceptions.
    /// </summary>
    /// <param name="message">The message to log</param>
    /// <param name="memberName">Auto-populated with the calling member name</param>
    /// <param name="filePath">Auto-populated with the calling file path</param>
    /// <param name="lineNumber">Auto-populated with the calling line number</param>
    public static void Error(
        string message,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        Write(LogLevel.Error, message, memberName, filePath, lineNumber);
    }

    /// <summary>
    /// Logs an exception with full details.
    /// </summary>
    /// <param name="ex">The exception to log</param>
    /// <param name="message">Optional additional context message</param>
    /// <param name="memberName">Auto-populated with the calling member name</param>
    /// <param name="filePath">Auto-populated with the calling file path</param>
    /// <param name="lineNumber">Auto-populated with the calling line number</param>
    public static void Exception(
        System.Exception ex,
        string message = null,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        var fullMessage = string.IsNullOrEmpty(message)
            ? $"{ex.GetType().Name}: {ex.Message}"
            : $"{message} - {ex.GetType().Name}: {ex.Message}";

        Write(LogLevel.Error, fullMessage, memberName, filePath, lineNumber);

        if (ex.InnerException != null)
        {
            Write(LogLevel.Error, $"  Inner Exception: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}", memberName, filePath, lineNumber);
        }

        if (!string.IsNullOrEmpty(ex.StackTrace))
        {
            Write(LogLevel.Debug, $"  Stack Trace: {ex.StackTrace}", memberName, filePath, lineNumber);
        }
    }

    /// <summary>
    /// Writes a separator line for visual grouping in logs
    /// </summary>
    /// <param name="title">Optional title for the separator</param>
    public static void Separator(string title = null)
    {
        if (!IsEnabled) return;

        var line = string.IsNullOrEmpty(title)
            ? "=".PadRight(80, '=')
            : $"=== {title} ".PadRight(80, '=');

        System.Diagnostics.Debug.WriteLine(line);
    }

    private static void Write(
        LogLevel level,
        string message,
        string memberName,
        string filePath,
        int lineNumber)
    {
        if (!IsEnabled || level < MinimumLevel)
            return;

        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        var fileName = System.IO.Path.GetFileNameWithoutExtension(filePath);
        var levelStr = level.ToString().ToUpper().PadRight(7);

        // Add distinctive prefix to make filtering easier in Visual Studio Output window
        var logMessage = $"[MZLOG] [{timestamp}] {levelStr} [{fileName}.{memberName}] {message}";

        System.Diagnostics.Debug.WriteLine(logMessage);
    }
}
