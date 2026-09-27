using TACT.Enums;
using TACT.Models;

namespace TACT.DTOs;

public class ResponseWorkTaskDto
{
    public long Id { get; set; }
    
    public string? Title { get; set; }
    
    public DateTime StartTime { get; set; } =  DateTime.UtcNow;
    
    public DateTime EndTime { get; set; }
    
    public List<ClientAppsDto> BlockedApps { get; set; } = new();
    
}