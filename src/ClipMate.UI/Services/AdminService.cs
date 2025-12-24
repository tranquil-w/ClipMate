using ClipMate.Service.Interfaces;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Security.Principal;

namespace ClipMate.Services
{
    public class AdminService : IAdminService
    {
        public bool IsRunningAsAdministrator()
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        public bool RestartAsAdministrator()
        {
            var exePath = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName;
            if (string.IsNullOrEmpty(exePath))
            {
                throw new InvalidOperationException("无法获取当前可执行文件路径");
            }

            try
            {
                var arguments = string.Join(" ", Environment.GetCommandLineArgs().Skip(1).Select(arg => $"\"{arg}\""));
                var startInfo = new ProcessStartInfo(exePath, arguments)
                {
                    UseShellExecute = true,
                    Verb = "runas"
                };

                Process.Start(startInfo);
                return true;
            }
            catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
            {
                return false;
            }
        }
    }
}
