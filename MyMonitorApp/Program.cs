using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyMonitorApp.Services;
using Serilog;
using System;
using System.IO;
using System.Threading.Tasks;

// Serilog の設定
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File(@"C:\logs\myapp-monitor.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    const string mutexName = "Global\\MyMonitorAppMutex";
    using Mutex mutex = new Mutex(true, mutexName, out bool isNewInstance);

    if (!isNewInstance)
    {
        Log.Information("すでに別のインスタンスが実行中です。終了します。");
        return; // 他のインスタンスが動作している場合、実行しない
    }

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
