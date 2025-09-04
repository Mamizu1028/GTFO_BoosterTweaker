using TheArchive.Interfaces;

namespace Hikaria.BoosterTweaker;

internal static class Logs
{
    private static IArchiveLogger _logger;

    public static void Setup(IArchiveLogger logger)
    {
        _logger = logger;
    }

    public static void LogDebug(object data)
    {
        _logger.Debug(data.ToString());
    }

    public static void LogError(object data)
    {
        _logger.Error(data.ToString());
    }

    public static void LogInfo(object data)
    {
        _logger.Info(data.ToString());
    }

    public static void LogMessage(object data)
    {
        _logger.Msg(ConsoleColor.White, data.ToString());
    }

    public static void LogWarning(object data)
    {
        _logger.Warning(data.ToString());
    }

    public static void LogNotice(object data)
    {
        _logger.Notice(data.ToString());
    }

    public static void LogSuccess(object data)
    {
        _logger.Success(data.ToString());
    }

    public static void LogException(Exception ex)
    {
        _logger.Exception(ex);
    }
}
