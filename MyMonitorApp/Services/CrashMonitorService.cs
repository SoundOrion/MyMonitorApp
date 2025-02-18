using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MyMonitorApp.Services;

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class CrashMonitorService : BackgroundService
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

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("クラッシュ監視サービスを開始...");

        while (!stoppingToken.IsCancellationRequested)
        {
            if (CheckForCrash())
            {
                string message = $"{_appName} がクラッシュしました。再起動します。";
                _logger.LogWarning(message);

                // 通知送信
                await _notificationService.SendAsync("MyApp クラッシュ通知", message);

                // プロセスを強制終了してから再起動
                RestartApp();
            }

            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }

    private bool CheckForCrash()
    {
        EventLog eventLog = new EventLog("Application");
        foreach (EventLogEntry entry in eventLog.Entries)
        {
            if ((entry.InstanceId == 1000 || entry.InstanceId == 1001 || entry.InstanceId == 1002 || entry.InstanceId == 7031) &&
                entry.Message.Contains(_appName + ".exe"))
            {
                _logger.LogError($"クラッシュ検出: {entry.TimeGenerated} | メッセージ: {entry.Message}");
                return true;
            }
        }
        return false;
    }

    private void RestartApp()
    {
        _logger.LogInformation($"{_appName} を停止して再起動します...");

        // 既存のプロセスを強制終了
        foreach (var process in Process.GetProcessesByName(_appName))
        {
            try
            {
                _logger.LogWarning($"プロセス {process.Id} を強制終了します...");
                process.Kill();
                process.WaitForExit(5000);
            }
            catch (Exception ex)
            {
                _logger.LogError($"プロセスの終了に失敗: {ex.Message}");
            }
        }

        // アプリケーションの再起動
        Process.Start(_appPath);
        _logger.LogInformation($"{_appName} を再起動しました。");
    }
}


