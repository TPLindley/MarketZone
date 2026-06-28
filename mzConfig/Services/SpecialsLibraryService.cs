using System.Text.Json;
using mzConfigure.Models;

namespace mzConfigure.Services;

/// <summary>
/// Manages a persistent JSON library of all specials ever used.
/// Automatically filters out duplicates based on text (case-insensitive).
/// </summary>
public class SpecialsLibraryService
{
    private const string LibraryFileName = "specials_library.json";
    private readonly string _libraryFilePath;

    public SpecialsLibraryService()
    {
        _libraryFilePath = Path.Combine(FileSystem.AppDataDirectory, LibraryFileName);
    }

    /// <summary>
    /// Load all specials from the library file
    /// </summary>
    public async Task<List<Special>> LoadLibraryAsync()
    {
        try
        {
            if (!File.Exists(_libraryFilePath))
                return new List<Special>();

            var json = await File.ReadAllTextAsync(_libraryFilePath);
            var specials = JsonSerializer.Deserialize<List<Special>>(json);
            return specials ?? new List<Special>();
        }
        catch
        {
            // If file is corrupted or can't be read, return empty list
            return new List<Special>();
        }
    }

    /// <summary>
    /// Add new specials to the library, filtering out duplicates.
    /// Duplicates are detected by Text property (case-insensitive).
    /// If a duplicate exists with different color, the existing entry is kept.
    /// </summary>
    public async Task AddToLibraryAsync(IEnumerable<Special> newSpecials)
    {
        // Load existing library
        var library = await LoadLibraryAsync();

        // Create a dictionary for fast duplicate lookup (case-insensitive)
        var libraryDict = library.ToDictionary(
            s => s.Text.Trim().ToLowerInvariant(),
            s => s,
            StringComparer.OrdinalIgnoreCase);

        // Add only non-duplicates
        foreach (var special in newSpecials)
        {
            var key = special.Text.Trim().ToLowerInvariant();
            if (!libraryDict.ContainsKey(key))
            {
                library.Add(special);
                libraryDict[key] = special;
            }
            // If duplicate with different color, ignore the new one (keep existing)
        }

        // Save updated library
        await SaveLibraryAsync(library);
    }

    /// <summary>
    /// Save the entire library to JSON file
    /// </summary>
    private async Task SaveLibraryAsync(List<Special> library)
    {
        try
        {
            var json = JsonSerializer.Serialize(library, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            await File.WriteAllTextAsync(_libraryFilePath, json);
        }
        catch
        {
            // Silently fail if we can't save (disk full, permissions, etc.)
            // The app can continue without library persistence
        }
    }

    /// <summary>
    /// Clear the entire library (for testing or user request)
    /// </summary>
    public async Task ClearLibraryAsync()
    {
        try
        {
            if (File.Exists(_libraryFilePath))
            {
                File.Delete(_libraryFilePath);
            }
        }
        catch
        {
            // Silently fail
        }
        await Task.CompletedTask;
    }

    /// <summary>
    /// Get the count of items in the library
    /// </summary>
    public async Task<int> GetLibraryCountAsync()
    {
        var library = await LoadLibraryAsync();
        return library.Count;
    }
}

