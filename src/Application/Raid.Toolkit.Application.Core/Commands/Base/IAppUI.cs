using System;

using Raid.Toolkit.Extensibility;

namespace Raid.Toolkit.Application.Core.Commands.Base
{
    public interface IAppUI : IDisposable
    {
        SynchronizationContext? SynchronizationContext { get; }
        void Run();
        void Dispatch(Action task);

        Task<bool> ShowExtensionInstaller(ExtensionBundle bundleToInstall);
        void ShowMain();
        void ShowAccounts();
        void ShowSettings();
        void ShowExtensionManager();
    }
}
