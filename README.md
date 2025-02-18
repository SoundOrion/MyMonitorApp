### **Serilog を導入し、ログの一元管理 & ログファイル出力を実装**
✅ **`ILogger` を活用し、Serilog を導入**  
✅ **コンソール + ログファイル +（オプションで JSON や DB などにも出力可能）**  
✅ **`Log(message);` の代わりに、Serilog で統一的に管理**

---

## **1. Serilog の導入**
**NuGet で以下をインストール**
```sh
dotnet add package Serilog
dotnet add package Serilog.Sinks.Console
dotnet add package Serilog.Sinks.File
dotnet add package Serilog.Extensions.Logging
```

---
## **2. `CrashMonitorService` を修正**
- **`ILogger` を `Serilog` で管理**
- **`Log(message);` を削除し、Serilog でログ出力**

### **📌 `Services/CrashMonitorService.cs`（修正後）**
```csharp
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
```
---
## **3. `Program.cs` で Serilog を設定**
- **Serilog を `ILogger` に統合**
- **コンソール + ログファイル出力を設定**
- **オプションで JSON 形式や DB にも保存可能**

### **📌 `Program.cs`（Serilog の設定を追加）**
```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.IO;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        // Serilog の設定
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File("C:\\logs\\myapp-monitor.log", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        try
        {
            Log.Information("MyMonitorApp を開始...");

            var host = Host.CreateDefaultBuilder(args)
                .UseSerilog() // Serilog をログシステムに統合
                .ConfigureServices((context, services) =>
                {
                    services.AddSingleton<INotificationService, EmailNotificationService>(); // 通知サービス
                    services.AddHostedService<CrashMonitorService>(); // クラッシュ監視
                })
                .Build();

            await host.RunAsync();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "アプリケーションが異常終了しました。");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}
```

---
## **4. 修正のポイント**
✅ **Serilog を `ILogger` に統合し、すべてのログを一元管理**  
✅ **`WriteTo.Console()` でリアルタイム出力し、`WriteTo.File()` でログファイル保存**  
✅ **ログを JSON やデータベースに保存することも可能（後述）**  
✅ **`Log.Fatal()` を使って、アプリの異常終了を記録**

---

## **5. ログの出力イメージ**
### **📌 `C:\logs\myapp-monitor.log` に出力**
```
2024-02-18 15:00:00 [INF] MyMonitorApp を開始...
2024-02-18 15:05:00 [INF] クラッシュ監視サービスを開始...
2024-02-18 15:10:00 [WRN] MyApp がクラッシュしました。再起動します。
2024-02-18 15:10:01 [WRN] プロセス 1234 を強制終了します...
2024-02-18 15:10:02 [INF] MyApp を再起動しました。
```

---
## **6. さらに拡張するなら？**
### **✅ (1) ログを JSON 形式にする**
Serilog の JSON Sink を利用すれば、ログを JSON 形式で保存可能。
```sh
dotnet add package Serilog.Formatting.Compact
```
設定を変更：
```csharp
.WriteTo.File(new CompactJsonFormatter(), "C:\\logs\\myapp-monitor.json", rollingInterval: RollingInterval.Day)
```

---
### **✅ (2) ログをデータベースに保存**
SQL Server にログを保存したい場合：
```sh
dotnet add package Serilog.Sinks.MSSqlServer
```
設定：
```csharp
.WriteTo.MSSqlServer(
    connectionString: "Server=myServer;Database=Logs;User Id=myUser;Password=myPassword;",
    sinkOptions: new MSSqlServerSinkOptions { TableName = "AppLogs", AutoCreateSqlTable = true })
```

---
### **✅ (3) メールでエラーログを送信**
Serilog の Email Sink を利用：
```sh
dotnet add package Serilog.Sinks.Email
```
設定：
```csharp
.WriteTo.Email(new EmailConnectionInfo
{
    FromEmail = "alert@example.com",
    ToEmail = "admin@example.com",
    MailServer = "smtp.example.com",
    NetworkCredentials = new NetworkCredential("your_email@example.com", "your_password"),
    EnableSsl = true
})
```

---
## **7. まとめ**
✅ **`Log(message);` を削除し、Serilog で統一管理！**  
✅ **コンソール + ログファイル +（オプションで JSON や DB）にも出力可能！**  
✅ **アプリが異常終了した場合 `Log.Fatal()` で記録！**  
✅ **ログがしっかり管理されることで、運用負担が大幅に軽減！**

🚀 **これで、C# の `IHost` + Serilog を活用した "クラッシュ監視 & 自動復旧 & 強力なログ管理システム" が完成！** 🎉