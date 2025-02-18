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

        // 既存のプロセスを Kill（存在しなくても問題なし）
        KillExistingProcess();

        // `MyApp` を再起動
        RestartApp();

        // 通知送信
        string message = $"{_appName} がクラッシュしました。再起動しました。";
        await _notificationService.SendAsync("MyApp 再起動通知", message);

        _logger.LogInformation("処理完了 - アプリを終了");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask; // タスクスケジューラ実行後に即終了
    }

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

    private void RestartApp()
    {
        _logger.LogInformation($"{_appName} を起動します...");
        Process.Start(_appPath);
    }
}
