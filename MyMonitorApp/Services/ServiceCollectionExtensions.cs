using Microsoft.Extensions.DependencyInjection;

namespace MyMonitorApp.Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNotificationService(this IServiceCollection services)
    {
        // ここで通知サービスを一括登録
        services.AddSingleton<INotificationService, EmailNotificationService>();

        // Slack 通知を追加したいならここで
        // services.AddSingleton<INotificationService, SlackNotificationService>();

        return services;
    }
}
