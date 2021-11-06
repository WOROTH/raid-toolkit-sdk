using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Raid.Service
{
    public class MainService : BackgroundService
    {
        private readonly RaidInstanceFactory Factory;
        private readonly ILogger<MainService> Logger;
        private readonly IHostApplicationLifetime Lifetime;
        private readonly UpdateService UpdateService;
        private readonly IServiceProvider ServiceProvider;

        public MainService(
            ProcessWatcherService processWatcher,
            ILogger<MainService> logger,
            RaidInstanceFactory factory,
            UpdateService updateService,
            IServiceProvider serviceProvider,
            IHostApplicationLifetime appLifetime,
            MemoryLogger memoryLogger)
        {
            Factory = factory;
            Logger = logger;
            Lifetime = appLifetime;
            UpdateService = updateService;
            ServiceProvider = serviceProvider;
            processWatcher.ProcessFound += (object sender, ProcessWatcherService.ProcessWatcherEventArgs e) =>
            {
                try
                {
                    Factory.Create(e.Process, serviceProvider.CreateScope());
                }
                catch (Exception ex)
                {
                    Logger.LogError(ServiceError.AccoutNotReady.EventId(), ex, "Account is not ready");
                    e.Retry = true;
                }
            };
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                await Task.Delay(-1, stoppingToken);
            }
            catch { }
        }

        public void Run(RunOptions options)
        {
            if (!options.Standalone)
                RegisterAction.RegisterProtocol(true);

            Console.CancelKeyPress += (sender, e) => Application.Exit();
            TaskExtensions.RunAfter(1, UpdateAccounts);

            Application.Run(ServiceProvider.GetRequiredService<UI.MainWindow>());
        }

        public void Restart()
        {
            Process.Start(AppConfiguration.ExecutablePath, new string[] { "--wait", "30000" });
            Exit();
        }

        public void Exit()
        {
            TaskExtensions.Shutdown();
            Application.Exit();
            Lifetime.StopApplication();
        }

        public async void InstallUpdate(GitHub.Schema.Release release)
        {
            try
            {
                await UpdateService.InstallRelease(release);
                Restart();
            }
            catch { }
        }

        private void OnClose(object sender, EventArgs e)
        {
            Exit();
        }

        private void UpdateAccounts()
        {
            foreach (var instance in Factory.Instances.Values)
            {
                try
                {
                    instance.Update();
                }
                catch (Exception ex)
                {
                    Logger.LogError(ServiceError.AccountUpdateFailed.EventId(), ex, $"Failed to update account {instance.Id}");
                }
            }
            TaskExtensions.RunAfter(10000, UpdateAccounts);
        }
    }
}