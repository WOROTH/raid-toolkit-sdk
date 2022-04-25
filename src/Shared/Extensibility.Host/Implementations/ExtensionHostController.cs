using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Raid.Toolkit.Extensibility.Host
{
    public class ExtensionHostController : IExtensionHostController, IDisposable
    {
        private readonly IModelLoader ModelLoader;
        private readonly IPackageLoader PackageLoader;
        private readonly IPackageManager Locator;
        private readonly IServiceProvider ServiceProvider;
        private readonly Dictionary<string, ExtensionHost> ExtensionPackages = new();
        private readonly Dictionary<Type, IDisposable> Instances = new();
        private bool IsDisposed;

        public ExtensionHostController(
            IPackageManager locator,
            IPackageLoader loader,
            IModelLoader modelLoader,
            IServiceProvider serviceProvider
            )
        {
            Locator = locator;
            PackageLoader = loader;
            ModelLoader = modelLoader;
            ServiceProvider = serviceProvider;
        }

        #region IExtensionHostController
        public async Task LoadExtensions()
        {
            foreach (var pkg in Locator.GetAllPackages())
                ExtensionPackages.Add(pkg.Id, ExtensionHost.CreateHost(ServiceProvider, PackageLoader.LoadPackage(pkg), pkg));

            var typePatterns = ExtensionPackages.Values.OfType<IRequireCodegen>().SelectMany(cg => cg.TypeFilter.IncludeTypes);
            _ = await Task.Run(() => ModelLoader.BuildAndLoad(typePatterns, false));
        }

        public void ActivateExtensions()
        {
            foreach (var pkg in ExtensionPackages.Values)
                pkg.Activate();
        }

        public void ShowExtensionUI()
        {
            foreach (var pkg in ExtensionPackages.Values)
                pkg.ShowUI();
        }

        public void DeactivateExtensions()
        {
            foreach (var pkg in ExtensionPackages.Values)
                pkg.Deactivate();

            ExtensionPackages.Clear();
            Instances.Clear();
        }

        public void InstallPackage(ExtensionBundle descriptor, bool activate)
        {
            ExtensionBundle installedPkg = Locator.AddPackage(descriptor);
            var pkg = PackageLoader.LoadPackage(installedPkg);
            ExtensionHost host = ExtensionHost.CreateHost(ServiceProvider, pkg, installedPkg);
            host.Install();
            ExtensionPackages.Add(installedPkg.Id, host);
        }

        public void UninstallPackage(string packageId)
        {
            if (ExtensionPackages.Remove(packageId, out var pkg))
            {
                pkg.Deactivate();
                pkg.Uninstall();
            }
        }
        #endregion

        #region IDisposable
        protected virtual void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    // dispose managed state (managed objects)
                    DeactivateExtensions();
                }

                IsDisposed = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}