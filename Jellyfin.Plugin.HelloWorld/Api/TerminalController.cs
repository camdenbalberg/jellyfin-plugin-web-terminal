using System;
using System.Diagnostics;
using System.Net.Mime;
using System.Threading.Tasks;
using MediaBrowser.Common.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.HelloWorld.Api;

/// <summary>
/// API controller for remote terminal execution.
/// </summary>
[ApiController]
[Authorize(Policy = Policies.RequiresElevation)]
[Route("HelloWorld")]
[Produces(MediaTypeNames.Application.Json)]
public class TerminalController : ControllerBase
{
    private readonly ILogger<TerminalController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TerminalController"/> class.
    /// </summary>
    public TerminalController(ILogger<TerminalController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Executes a command on the server.
    /// </summary>
    /// <param name="request">The command to execute.</param>
    /// <returns>The command output and exit code.</returns>
    [HttpPost("Execute")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<CommandResponse>> Execute([FromBody] CommandRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Command))
        {
            return Ok(new CommandResponse
            {
                Output = string.Empty,
                Error = "No command provided.",
                ExitCode = -1
            });
        }

        var config = Plugin.Instance?.Configuration;
        var timeoutMs = (config?.CommandTimeoutSeconds ?? 30) * 1000;
        var shell = config?.ShellPath ?? "cmd.exe";
        var shellArgs = config?.ShellArgs ?? "/c";

        _logger.LogInformation("Executing command: {Command}", request.Command);

        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = shell,
                Arguments = $"{shellArgs} {request.Command}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = request.WorkingDirectory ?? "C:\\"
            };

            process.Start();

            var stdoutTask = process.StandardOutput.ReadToEndAsync();
            var stderrTask = process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync().WaitAsync(TimeSpan.FromMilliseconds(timeoutMs)).ConfigureAwait(false);

            var stdout = await stdoutTask.ConfigureAwait(false);
            var stderr = await stderrTask.ConfigureAwait(false);

            return Ok(new CommandResponse
            {
                Output = stdout,
                Error = stderr,
                ExitCode = process.ExitCode
            });
        }
        catch (TimeoutException)
        {
            return Ok(new CommandResponse
            {
                Output = string.Empty,
                Error = $"Command timed out after {timeoutMs / 1000} seconds.",
                ExitCode = -1
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing command");
            return Ok(new CommandResponse
            {
                Output = string.Empty,
                Error = ex.Message,
                ExitCode = -1
            });
        }
    }
}

/// <summary>
/// Request model for command execution.
/// </summary>
public class CommandRequest
{
    /// <summary>
    /// Gets or sets the command to execute.
    /// </summary>
    public string Command { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the working directory.
    /// </summary>
    public string? WorkingDirectory { get; set; }
}

/// <summary>
/// Response model for command execution.
/// </summary>
public class CommandResponse
{
    /// <summary>
    /// Gets or sets the standard output.
    /// </summary>
    public string Output { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the standard error.
    /// </summary>
    public string Error { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the exit code.
    /// </summary>
    public int ExitCode { get; set; }
}
