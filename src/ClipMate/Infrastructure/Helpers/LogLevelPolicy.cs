using System.Collections.Generic;
using Serilog.Events;

namespace ClipMate.Infrastructure
{
    internal static class LogLevelPolicy
    {
        private static readonly HashSet<LogEventLevel> _supportedLevels = new()
        {
            LogEventLevel.Error,
            LogEventLevel.Warning,
            LogEventLevel.Information,
            LogEventLevel.Debug
        };

        public static bool IsSupported(LogEventLevel level) => _supportedLevels.Contains(level);

        public static LogEventLevel Normalize(LogEventLevel level)
        {
            return IsSupported(level) ? level : LogEventLevel.Information;
        }
    }
}
