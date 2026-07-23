namespace TACT.DTOs;

public class RawTaskResponseDto
{
    public string Title { get; set; } = string.Empty;
    public string? StartTime { get; set; }
    public string? EndTime { get; set; }
    public List<string> BlockedPackageNames { get; set; } = new();
}