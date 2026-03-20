using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
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
public class TerminalController : ControllerBase
{
    private static readonly ConcurrentDictionary<string, (Process Process, CancellationTokenSource Cts)> _running = new();

    private readonly ILogger<TerminalController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TerminalController"/> class.
    /// </summary>
    public TerminalController(ILogger<TerminalController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Executes a command and streams output as Server-Sent Events.
    /// </summary>
    [HttpPost("Execute")]
    public async Task Execute([FromBody] CommandRequest request)
    {
        Response.ContentType = "text/event-stream";
        Response.Headers["Cache-Control"] = "no-cache";
        Response.Headers["Connection"] = "keep-alive";

        if (!ValidateApiKey())
        {
            Response.StatusCode = 403;
            await WriteSSE("error", "Invalid or missing API key.").ConfigureAwait(false);
            return;
        }

        if (string.IsNullOrWhiteSpace(request.Command))
        {
            await WriteSSE("error", "No command provided.").ConfigureAwait(false);
            await WriteSSE("exit", "-1").ConfigureAwait(false);
            return;
        }

        var config = Plugin.Instance?.Configuration;
        var timeoutMs = (config?.CommandTimeoutSeconds ?? 300) * 1000;
        var shell = config?.ShellPath ?? "cmd.exe";
        var shellArgs = config?.ShellArgs ?? "/c";

        var sessionId = request.SessionId ?? Guid.NewGuid().ToString("N");

        _logger.LogInformation("Executing command (session {SessionId}): {Command}", sessionId, request.Command);

        await WriteSSE("session", sessionId).ConfigureAwait(false);

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

            using var cts = new CancellationTokenSource(timeoutMs);
            _running[sessionId] = (process, cts);

            var token = cts.Token;

            try
            {
                var stdoutTask = ReadStreamLines(process.StandardOutput, "stdout", token);
                var stderrTask = ReadStreamLines(process.StandardError, "stderr", token);

                await Task.WhenAll(stdoutTask, stderrTask).ConfigureAwait(false);

                await process.WaitForExitAsync(token).ConfigureAwait(false);
                await WriteSSE("exit", process.ExitCode.ToString()).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                try
                {
                    process.Kill(true);
                }
                catch
                {
                    // Best effort kill
                }

                await WriteSSE("error", "Command cancelled.").ConfigureAwait(false);
                await WriteSSE("exit", "-1").ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing command");
            await WriteSSE("error", ex.Message).ConfigureAwait(false);
            await WriteSSE("exit", "-1").ConfigureAwait(false);
        }
        finally
        {
            _running.TryRemove(sessionId, out _);
        }
    }

    /// <summary>
    /// Cancels a running command by session ID.
    /// </summary>
    [HttpPost("Cancel")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult Cancel([FromBody] CancelRequest request)
    {
        if (!ValidateApiKey())
        {
            return StatusCode(403, "Invalid or missing API key.");
        }

        if (string.IsNullOrWhiteSpace(request.SessionId))
        {
            return NotFound("No session ID provided.");
        }

        if (_running.TryRemove(request.SessionId, out var entry))
        {
            _logger.LogInformation("Cancelling session {SessionId}", request.SessionId);
            try
            {
                entry.Cts.Cancel();
                entry.Process.Kill(true);
            }
            catch
            {
                // Best effort
            }

            return Ok(new { Cancelled = true });
        }

        return NotFound("Session not found or already finished.");
    }

    /// <summary>
    /// Validates the API key from the request header. Used by the frontend to verify the key.
    /// </summary>
    [HttpPost("ValidateKey")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public ActionResult ValidateKeyEndpoint()
    {
        if (!ValidateApiKey())
        {
            return StatusCode(403, "Invalid or missing API key.");
        }

        return Ok(new { Valid = true });
    }

    private bool ValidateApiKey()
    {
        var configuredKey = Plugin.Instance?.Configuration?.ApiKey;
        if (string.IsNullOrEmpty(configuredKey))
        {
            return false;
        }

        var providedKey = Request.Headers["X-Terminal-Key"].ToString();
        return string.Equals(providedKey, configuredKey, StringComparison.Ordinal);
    }

    private async Task ReadStreamLines(StreamReader reader, string eventType, CancellationToken cancellationToken)
    {
        try
        {
            string? line;
            while ((line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false)) != null)
            {
                await WriteSSE(eventType, line).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // Cancelled - handled by caller
        }
    }

    private async Task WriteSSE(string eventType, string data)
    {
        var message = $"event: {eventType}\ndata: {data.Replace("\n", "\ndata: ")}\n\n";
        await Response.WriteAsync(message, Encoding.UTF8).ConfigureAwait(false);
        await Response.Body.FlushAsync().ConfigureAwait(false);
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

    /// <summary>
    /// Gets or sets the session ID for cancellation support.
    /// </summary>
    public string? SessionId { get; set; }
}

/// <summary>
/// Request model for cancelling a command.
/// </summary>
public class CancelRequest
{
    /// <summary>
    /// Gets or sets the session ID to cancel.
    /// </summary>
    public string SessionId { get; set; } = string.Empty;
}
