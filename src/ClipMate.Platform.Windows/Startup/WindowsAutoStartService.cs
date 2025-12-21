using ClipMate.Platform.Abstractions.Startup;
using Microsoft.Win32;
using Serilog;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Principal;

namespace ClipMate.Platform.Windows.Startup;

public sealed class WindowsAutoStartService : IAutoStartService
{
    private const string AppName = "ClipMate";
    private const string RegistryKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string TaskSchedulerName = "ClipMate_AutoStart";

    private readonly ILogger _logger;

    public WindowsAutoStartService(ILogger logger)
    {
        _logger = logger;
    }

    public bool IsAutoStartEnabled()
    {
        return GetCurrentMethod() != AutoStartMethod.None;
    }

    public AutoStartMethod GetCurrentMethod()
    {
        try
        {
            if (IsTaskSchedulerAutoStartEnabled())
            {
                return AutoStartMethod.TaskScheduler;
            }

            if (IsRegistryAutoStartEnabled())
            {
                return AutoStartMethod.Registry;
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "检测自启动方式时发生错误");
        }

        return AutoStartMethod.None;
    }

    public void SetAutoStart(bool enabled)
    {
        try
        {
            if (enabled)
            {
                if (IsRunningAsAdministrator())
                {
                    SetAutoStartWithRegistry(false);
                    SetAutoStartWithTaskScheduler(true);
                    return;
                }

                SetAutoStartWithTaskScheduler(false);
                SetAutoStartWithRegistry(true);
                return;
            }

            SetAutoStartWithTaskScheduler(false);
            SetAutoStartWithRegistry(false);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.Error(ex, "没有足够的权限修改自启动设置");
            throw new InvalidOperationException("无法修改自启动设置，请检查权限", ex);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "修改自启动设置时发生错误");
            throw;
        }
    }

    private static bool IsRunningAsAdministrator()
    {
        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }

    private static string GetExecutablePath()
    {
        var exePath = Environment.ProcessPath ?? Assembly.GetExecutingAssembly().Location;
        return Path.GetFullPath(exePath);
    }

    private bool IsRegistryAutoStartEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, false);
            var value = key?.GetValue(AppName) as string;
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            var cleanedValue = value.Trim('"');
            return Path.GetFullPath(cleanedValue).Equals(GetExecutablePath(), StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "检查注册表自启动状态时发生错误");
            return false;
        }
    }

    private void SetAutoStartWithRegistry(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RegistryKeyPath, true);
        if (key == null)
        {
            throw new InvalidOperationException("无法创建或打开注册表键");
        }

        if (enabled)
        {
            var exePath = GetExecutablePath();
            key.SetValue(AppName, $"\"{exePath}\"");
            _logger.Information("已通过注册表启用开机自启动: {Path}", exePath);
            return;
        }

        if (key.GetValue(AppName) != null)
        {
            key.DeleteValue(AppName, false);
        }

        _logger.Information("已通过注册表禁用开机自启动");
    }

    private bool IsTaskSchedulerAutoStartEnabled()
    {
        try
        {
            var result = RunSchtasks($"/Query /TN \"{TaskSchedulerName}\"");
            return result.ExitCode == 0;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "检查任务计划程序自启动状态时发生错误");
            return false;
        }
    }

    private void SetAutoStartWithTaskScheduler(bool enabled)
    {
        if (enabled)
        {
            if (!IsRunningAsAdministrator())
            {
                throw new InvalidOperationException("需要管理员权限才能使用任务计划程序配置自启动");
            }

            var exePath = GetExecutablePath();
            var result = RunSchtasks($"/Create /TN \"{TaskSchedulerName}\" /TR \"\\\"{exePath}\\\"\" /SC ONLOGON /RL HIGHEST /F");
            if (result.ExitCode != 0)
            {
                throw new InvalidOperationException($"创建任务计划程序自启动失败: {result.StandardError}");
            }

            _logger.Information("已通过任务计划程序启用开机自启动: {Path}", exePath);
            return;
        }

        if (!IsTaskSchedulerAutoStartEnabled())
        {
            return;
        }

        var (ExitCode, StandardOutput, StandardError) = RunSchtasks($"/Delete /TN \"{TaskSchedulerName}\" /F");
        if (ExitCode != 0)
        {
            _logger.Warning("删除任务计划程序自启动任务失败: {Error}", StandardError);
            return;
        }

        _logger.Information("已删除任务计划程序自启动任务");
    }

    private (int ExitCode, string StandardOutput, string StandardError) RunSchtasks(string arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "schtasks",
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("无法启动 schtasks 进程");

        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        _logger.Debug("schtasks {Arguments} => ExitCode {ExitCode}, Output: {Output}, Error: {Error}",
            arguments, process.ExitCode, output, error);

        return (process.ExitCode, output, error);
    }
}
