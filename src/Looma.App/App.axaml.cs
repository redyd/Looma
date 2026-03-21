using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Looma.Infrastructure;
using Looma.Infrastructure.Storage;
using Looma.Presentation.ViewModels.Main;
using Looma.Views.Views.Main;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Looma.App;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();
        services.AddPresentation();
        services.AddInfrastructure();
        
        services.AddDbContext<LoomaDbContext>(options =>
            options.UseSqlite($"Data Source={AppPaths.DatabasePath}"));
        
        Services = services.BuildServiceProvider();
        
        AppPaths.EnsureDirectoriesExist();
        
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LoomaDbContext>();
        AppPaths.EnsureDatabaseCreated(db);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = Services.GetRequiredService<MainViewModel>()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}