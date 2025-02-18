using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MyMonitorApp.Services;

public class CrashMonitorService : IHostedService
{
    private readonly ILogger<CrashMonitorService> _logger;
    private readonly INotificationService _notificationService;
    private readonly string _appName = "MyApp";
    private readonly string _appPath = @"C:\Program Files\MyApp\MyApp.exe";

    public CrashMonitorService(ILogger<CrashMonitorService> logger, INotificationService notificationService)
    {
        _logger = logger;
        _notificationService = notificationService;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation($"タスクスケジューラによる {_appName} のクラッシュ検知 - 即時再起動処理を開始");

        // 直近のクラッシュログを取得して記録
        string crashDetails = GetLatestCrashLog();
        if (!string.IsNullOrEmpty(crashDetails))
        {
            _logger.LogError($"クラッシュ検出: {crashDetails}");
        }
        else
        {
            _logger.LogWarning($"クラッシュログが見つかりませんでした。");
        }

        // 既存のプロセスを Kill（存在しなくても問題なし）
        KillExistingProcess();

        // `MyApp` を再起動
        RestartApp();

        // 通知送信
        string message = $"{_appName} がクラッシュしました。再起動しました。\n{crashDetails}";
        await _notificationService.SendAsync("MyApp 再起動通知", message);

        _logger.LogInformation("処理完了 - アプリを終了");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask; // タスクスケジューラ実行後に即終了
    }

    /// <summary>
    /// 直近のクラッシュログを取得
    /// </summary>
    private string GetLatestCrashLog()
    {
        try
        {
            using (EventLog eventLog = new EventLog("Application"))
            {
                var crashEntry = eventLog.Entries
                    .Cast<EventLogEntry>()
                    .Where(entry =>
                        (entry.InstanceId == 1000 || entry.InstanceId == 1001 || entry.InstanceId == 1002 || entry.InstanceId == 7031) &&
                        entry.Message.Contains(_appName + ".exe"))
                    .OrderByDescending(entry => entry.TimeGenerated)
                    .FirstOrDefault();

                if (crashEntry != null)
                {
                    return $"{crashEntry.TimeGenerated} | メッセージ: {crashEntry.Message}";
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"クラッシュログ取得中にエラー: {ex.Message}");
        }
        return string.Empty;
    }

    /// <summary>
    /// `MyApp` の既存プロセスを強制終了
    /// </summary>
    private void KillExistingProcess()
    {
        var processes = Process.GetProcessesByName(_appName);
        if (processes.Any())
        {
            foreach (var process in processes)
            {
                try
                {
                    _logger.LogWarning($"既存の {_appName} プロセス (ID: {process.Id}) を強制終了します...");
                    process.Kill();
                    process.WaitForExit(5000);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"プロセスの強制終了に失敗: {ex.Message}");
                }
            }
        }
        else
        {
            _logger.LogInformation($"{_appName} のプロセスは存在しませんでした。");
        }
    }

    /// <summary>
    /// `MyApp` を再起動
    /// </summary>
    private void RestartApp()
    {
        _logger.LogInformation($"{_appName} を起動します...");
        Process.Start(_appPath);
    }
}
