using Il2CppToolkit.Runtime;
using System;
using System.Diagnostics;

namespace Raid.Toolkit.Injection
{
	public class ClientApi : IDisposable
	{
		private readonly Process Process;
		private readonly Il2CsRuntimeContext Context;
		private bool disposedValue;

		public ClientApi(Process proc)
		{
			Process = proc;
			Context = new(proc);
		}

		public void InvokeOn<T>(T obj, CallMethodMessage call) where T : StructBase
		{
			Executor.InvokeInstanceFunction(Process.MainWindowHandle, obj.Address, call);
		}

		public void CallMethod<T>(T obj, string methodName, params ArgumentValue[] args) where T : StructBase
		{
			CallMethodMessage call = new()
			{
				cls = new() { szName = typeof(T).Name, szNamespace = typeof(T).Namespace ?? "" },
				fn = new() { szName = methodName, cParam = args.Length },
				args = Interop.TArrayOfLength(16, args),
			};
			Executor.InvokeInstanceFunction(Process.MainWindowHandle, obj.Address, call);
		}

		public void CallMethod<T>(string methodName, params ArgumentValue[] args) where T : StructBase
		{
			CallMethodMessage call = new()
			{
				cls = new() { szName = typeof(T).Name, szNamespace = typeof(T).Namespace ?? "" },
				fn = new() { szName = methodName, cParam = args.Length },
				args = Interop.TArrayOfLength(16, args),
			};
			Executor.InvokeStaticFunction(Process.MainWindowHandle, call);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
                Context?.Dispose();
				disposedValue = true;
			}
        }

		~ClientApi()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: false);
		}

		public void Dispose()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
	}
}