using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TACT.Data;
using TACT.DTOs;
using TACT.Enums;
using TACT.Models;
using TACT.Services;

namespace TACT.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class WorkTasksController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<WorkTasksController> _logger;
    private readonly UserManager<User> _userManager;
    private readonly AiService _aiService;

    public WorkTasksController(ApplicationDbContext context, ILogger<WorkTasksController> logger,  UserManager<User> userManager,AiService aiService)
    {
        _context = context;
        _logger = logger;
        _userManager = userManager;
        _aiService = aiService;
    }

    [HttpPost("create-task")]
    public async Task<IActionResult> CreateTask([FromBody] CreateTaskRequestDto workTask)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out long userId))
        {
            return Unauthorized();
        }
        
        var user = await _context.Users
            .Include(u => u.InstalledApps)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null) 
            return NotFound("User not found");

        if (user.InstalledApps == null || !user.InstalledApps.Any())
        {
            return BadRequest(new { message = "No installed apps found for this user. Please sync apps first." });
        }

        var userAppsDto = user.InstalledApps.Select(iApp => new ClientAppsDto
        {
            PackageName = iApp.PackageName,
            AppName = iApp.AppName
        }).ToList();

        var blockedAppsDto = await _aiService.GetBlockedAppsAsync(workTask.Title, userAppsDto);

        var newWorkTask = new WorkTask
        {
            Title = workTask.Title,
            StartTime = workTask.StartTime,
            EndTime = workTask.EndTime,
            Source = WorkTaskSource.Manual,
            UserId = user.Id,
            BlockedApps = blockedAppsDto.Select(b => new BlockedApp
            {
                PackageName = b.PackageName,
                AppName = b.AppName
            }).ToList()
        };
        
        user.WorkTasks.Add(newWorkTask);
        
        await _context.WorkTasks.AddAsync(newWorkTask);
        await _context.SaveChangesAsync();

        var response = new ResponseWorkTaskDto
        {
            Id =  newWorkTask.Id,
            Title = newWorkTask.Title,
            StartTime = newWorkTask.StartTime,
            EndTime = newWorkTask.EndTime,
            BlockedApps = newWorkTask.BlockedApps.Select(b => new ClientAppsDto
            {
                PackageName = b.PackageName,
                AppName = b.AppName
            }).ToList()
        };
        return Ok(response);
    }

    [HttpPost("create-tasks-ai")]
public async Task<IActionResult> CreateTasksAi([FromBody] VoiceTaskRequestDto request)
{
    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out long userId))
    {
        return Unauthorized();
    }

    if (string.IsNullOrWhiteSpace(request?.VoiceText))
    {
        return BadRequest(new { message = "Voice text cannot be empty." });
    }

    var user = await _context.Users
        .Include(u => u.InstalledApps)
        .FirstOrDefaultAsync(u => u.Id == userId);
    
    if (user == null) 
        return NotFound("User not found");

    if (user.InstalledApps == null || !user.InstalledApps.Any())
    {
        return BadRequest(new { message = "No installed apps found for this user. Please sync apps first." });
    }
    
    var userAppsDto = user.InstalledApps.Select(iApp => new ClientAppsDto
    {
        PackageName = iApp.PackageName,
        AppName = iApp.AppName
    }).ToList();
    
    var extractedAiTasks = await _aiService.ExtractTasksAndBlockedAppsAsync(request.VoiceText, userAppsDto);

    if (extractedAiTasks == null || !extractedAiTasks.Any())
    {
        return Ok(new List<ResponseWorkTaskDto>()); 
    }

    var newWorkTasks = extractedAiTasks.Select(aiTask => new WorkTask
    {
        Title = aiTask.Title,
        StartTime = aiTask.StartTime,
        EndTime = aiTask.EndTime,
        Source = WorkTaskSource.Ai, 
        UserId = user.Id,
        BlockedApps = aiTask.BlockedApps.Select(b => new BlockedApp
        {
            PackageName = b.PackageName,
            AppName = b.AppName
        }).ToList()
    }).ToList();

    await _context.WorkTasks.AddRangeAsync(newWorkTasks);
    await _context.SaveChangesAsync();

    var responseDtos = newWorkTasks.Select(task => new ResponseWorkTaskDto
    {
        Id = task.Id,
        Title = task.Title,
        StartTime = task.StartTime,
        EndTime = task.EndTime,
        BlockedApps = task.BlockedApps.Select(b => new ClientAppsDto
        {
            PackageName = b.PackageName,
            AppName = b.AppName
        }).ToList()
    }).ToList();

    return Ok(responseDtos);
}
    
}