using LmStudioServerAdmin.Commands;
using LmStudioServerAdmin.Logging;

namespace LmStudioServerAdmin.Service;

public class StatusChecker : IDisposable
{
    private readonly Timer? _timer;
    private readonly Action<LmsStatus> _statusChanged;

    public StatusChecker(Action<LmsStatus> statusChanged, int intervalSeconds = 60)
    {
        _statusChanged = statusChanged ?? throw new ArgumentNullException(nameof(statusChanged));

        _timer = new Timer(
            callback: _ => CheckStatus(),
            state: null,
            dueTime: TimeSpan.FromSeconds(10), // первая проверка через 10 сек
            period: TimeSpan.FromSeconds(intervalSeconds)
        );

        Logger.Info($"StatusChecker started with interval {intervalSeconds}s");
    }

    private void CheckStatus()
    {
        try
        {
            var status = LmsCommandExecutor.GetStatus();
            _statusChanged(status);
        }
        catch (Exception ex)
        {
            Logger.Error($"Error in StatusChecker: {ex.Message}", ex);
        }
    }

    public void Dispose()
    {
        _timer?.Dispose();
        Logger.Info("StatusChecker disposed");
    }
}
