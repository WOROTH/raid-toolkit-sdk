using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;

using Raid.Toolkit.Extensibility.DataServices;

namespace Raid.Toolkit.Extensibility.Host
{
    public class WindowManager : IWindowManager
    {
        public class WindowState
        {
            public WindowState()
            { }
            public WindowState(object owner, bool isVisible)
            {
                IWindowAdapter window = WrapWindow(owner);
                Visible = isVisible;
                Location = window.Location;
                Size = window.Size;
            }
            public bool Visible { get; set; }
            public Point Location;
            public Size Size;
        }

        private readonly Dictionary<Type, WindowOptions> Options = new();
        private readonly Dictionary<string, WindowState> States;
        private readonly IServiceProvider ServiceProvider;
        private readonly PersistedDataStorage Storage;
        private readonly ILogger Logger;

        public bool CanShowUI { get; set; } = true;

        public WindowManager(
            IServiceProvider serviceProvider,
            PersistedDataStorage storage,
            ILogger<WindowManager> logger
            )
        {
            ServiceProvider = serviceProvider;
            Storage = storage;
            Logger = logger;
            if (!Storage.TryRead(AppStateDataContext.Default, "windows", out States))
                States = new();
        }

        public void RestoreWindows()
        {
            foreach (var (type, options) in Options)
            {
                if (string.IsNullOrEmpty(type.FullName)) continue;

                if (options.RememberVisibility
                    && States.TryGetValue(type.FullName, out WindowState? state)
                    && state.Visible)
                {
#pragma warning disable CS0618 // Type or member is obsolete
                    IWindowAdapter window = CreateWindow(type);
#pragma warning restore CS0618 // Type or member is obsolete
                    window.Show();
                }
            }
        }

        private static IWindowAdapter<T> WrapWindow<T>(T instance) where T : class
        {
            return instance is IWindowAdapter<T> adapter
                ? adapter
                : (global::Raid.Toolkit.Extensibility.IWindowAdapter<T>)(instance is Form form
                ? new FormAdapter<T>(form)
                : instance is Window wnd ? new WinUIAdapter<T>(wnd) : throw new NotSupportedException());
        }

        public IWindowAdapter<T> CreateWindow<T>() where T : class
        {
            Logger.LogInformation("Creating window {type}", typeof(T).FullName);
            if (!Options.TryGetValue(typeof(T), out WindowOptions? options))
            {
                Logger.LogWarning("Type '{typeName}' is not registered, window preferences won't be persisted", typeof(T).FullName);
            }

            T instance = (T)(options?.CreateInstance != null
                ? options.CreateInstance()
                : ActivatorUtilities.CreateInstance(ServiceProvider, typeof(T)));

            IWindowAdapter<T> adapter = WrapWindow(instance);
            if (options != null)
                AttachEvents(options, adapter);

            return adapter;
        }

        private static readonly System.Reflection.MethodInfo? createWindowMethod = typeof(WindowManager).GetMethod("CreateWindow", 1, Array.Empty<Type>());
        [Obsolete("Use CreateWindow<T> instead")]
        public IWindowAdapter CreateWindow(Type type)
        {
            return createWindowMethod?.MakeGenericMethod(type).Invoke(this, null) is not IWindowAdapter adapter
                ? throw new InvalidCastException()
                : adapter;
        }

        private void AttachEvents(WindowOptions options, IWindowAdapter window)
        {
            if (window == null) return;
            if (options.RememberVisibility)
            {
                window.Closing += SetFormClosedState;
                window.Shown += SetFormVisibleState;
            }
            if (options.RememberPosition)
            {
                string? senderType = window.GetType().FullName;
                if (!string.IsNullOrEmpty(senderType) && States.TryGetValue(senderType, out WindowState? state) && state != null)
                {
                    window.Location = state.Location;
                }

                window.Resized += SetFormVisibleState;
            }
        }

        private void SetFormVisibleState(object? sender, WindowAdapterEventArgs e)
        {
            if (sender == null)
                return;

            Logger.LogInformation("Updating window {senderType} (open)", e.OwnerType);
            States[e.OwnerType] = new(sender, true);
            _ = Storage.Write(AppStateDataContext.Default, "windows", States);
        }

        private void SetFormClosedState(object? sender, WindowAdapterCloseEventArgs e)
        {
            if (sender == null)
                return;

            Logger.LogInformation("Updating window {senderType} (closed)", e.OwnerType);
            // if closing for application exit/shutdown, then consider it still open so it will re-open on next launch
            States[e.OwnerType] = new(sender, !e.IsUserClose);
            _ = Storage.Write(AppStateDataContext.Default, "windows", States);
        }

        public void RegisterWindow<T>(WindowOptions options) where T : class
        {
            Logger.LogInformation("Registered window {type}", typeof(T).FullName);
            Options.Add(typeof(T), options);
        }

        public void UnregisterWindow<T>() where T : class
        {
            Logger.LogInformation("Unregistered window {type}", typeof(T).FullName);
            _ = Options.Remove(typeof(T));
        }
    }
}
