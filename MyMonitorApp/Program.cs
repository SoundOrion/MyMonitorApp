using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyMonitorApp.Services;
using Serilog;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

// Serilog の設定
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File(@"C:\logs\myapp-monitor.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("MyMonitorApp を開始...");

    const string mutexName = "Global\\MyMonitorAppMutex";
    using Mutex mutex = new Mutex(true, mutexName, out bool isNewInstance);

    if (!isNewInstance)
    {
        Log.Information("すでに別のインスタンスが実行中です。終了します。");
        return; // 他のインスタンスが動作している場合、実行しない
    }

    var host = Host.CreateDefaultBuilder(args)
        .UseSerilog() // Serilog をログシステムに統合
        .ConfigureServices((context, services) =>
        {
            services.AddNotificationService(); // 通知サービスの追加
            services.AddHostedService<CrashMonitorService>(); // クラッシュ監視
        })
        .Build();

    // `using` の外で実行して Mutex を保持
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
