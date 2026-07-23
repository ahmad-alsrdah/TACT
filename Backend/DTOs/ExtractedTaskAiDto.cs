namespace TACT.DTOs;

public class ExtractedTaskAiDto
{
    
    public string Title { get; set; } = string.Empty;
    
    public DateTime StartTime { get; set; } = DateTime.UtcNow;
    
    public DateTime EndTime { get; set; } = DateTime.UtcNow.AddHours(1);
    
    public List<ClientAppsDto> BlockedApps { get; set; } = new();

}