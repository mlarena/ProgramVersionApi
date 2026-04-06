using System.Text.Json;

namespace ProgramVersionApi.Services;

public class ProgramService
{
    private readonly string _filePath;
    
    public ProgramService()
    {
        _filePath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "programs.json");
    }
    
    public async Task<string?> GetVersionByNameAsync(string programName)
    {
        if (!File.Exists(_filePath))
            return null;
            
        var json = await File.ReadAllTextAsync(_filePath);
        using var doc = JsonDocument.Parse(json);
        
        var root = doc.RootElement;
        if (root.TryGetProperty("programs", out var programs))
        {
            foreach (var program in programs.EnumerateArray())
            {
                if (program.TryGetProperty("ProgramName", out var name) &&
                    name.GetString()?.Equals(programName, StringComparison.OrdinalIgnoreCase) == true)
                {
                    return program.GetProperty("Version").GetString();
                }
            }
        }
        
        return null;
    }
}