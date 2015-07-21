using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Policy;

namespace SymbolBinder
{
    public class AssemblyReflectionProxy : MarshalByRefObject
    {
        private string _assFullName;
        private string _assemblyPath;

        public void LoadAssembly(string assemblyPath)
        {
            try
            {
                Assembly ass = Assembly.ReflectionOnlyLoadFrom(assemblyPath);
                _assFullName = ass.FullName;
                _assemblyPath = assemblyPath;
            }
            catch (FileNotFoundException ex)
            {
                // Continue loading assemblies even if an assembly can not be loaded in the new AppDomain.
            }
        }

        public TResult Reflect<TResult>(Func<Assembly, TResult> func)
        {
            DirectoryInfo directory = new FileInfo(_assemblyPath).Directory;
            ResolveEventHandler resolveEventHandler = (s, e) =>
                {
                    return OnReflectionOnlyResolve(e, directory);
                };

            AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += resolveEventHandler;

            Assembly assembly = null;
            foreach (var ass in AppDomain.CurrentDomain.ReflectionOnlyGetAssemblies())
            {
                if (ass.FullName.CompareTo(_assFullName) == 0)
                {
                    assembly = ass;
                    break;
                }
            }

            if (assembly == null)
                return default(TResult);

            var result = func(assembly);
            AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve -= resolveEventHandler;
            return result;
        }

        private Assembly OnReflectionOnlyResolve(ResolveEventArgs args, DirectoryInfo directory)
        {
            Assembly loadedAssembly = null;
            foreach (var ass in AppDomain.CurrentDomain.ReflectionOnlyGetAssemblies())
            {
                if (string.Equals(ass.FullName, args.Name, StringComparison.OrdinalIgnoreCase))
                {
                    loadedAssembly = ass;
                    break;
                }
            }

            if (loadedAssembly != null)
            {
                return loadedAssembly;
            }

            AssemblyName assemblyName = new AssemblyName(args.Name);
            string dependentAssemblyFilename = Path.Combine(directory.FullName, assemblyName.Name + ".dll");

            if (File.Exists(dependentAssemblyFilename))
            {
                return Assembly.ReflectionOnlyLoadFrom(dependentAssemblyFilename);
            }
            return Assembly.ReflectionOnlyLoad(args.Name);
        }
    }

    public class AssemblyReflectionManager : IDisposable
    {
        AppDomain _appDomain;
        AssemblyReflectionProxy _proxy;

        public bool LoadAssembly(string assemblyPath, string domainName)
        {
            _appDomain = CreateChildDomain(AppDomain.CurrentDomain, domainName);

            // load the assembly in the specified app domain
            try
            {
                Type proxyType = typeof(AssemblyReflectionProxy);
                if (proxyType.Assembly != null)
                {
                    _proxy = (AssemblyReflectionProxy)_appDomain.CreateInstanceFromAndUnwrap(proxyType.Assembly.Location, proxyType.FullName);
                    _proxy.LoadAssembly(assemblyPath);
                    return true;
                }
            }
            catch (Exception ex)
            {
                throw;
            }

            return false;
        }

        public TResult Reflect<TResult>(Func<Assembly, TResult> func)
        {
            if (_proxy == null)
                return default(TResult);
            return _proxy.Reflect(func);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~AssemblyReflectionManager()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _proxy = null;
                AppDomain.Unload(_appDomain);
                _appDomain = null;
            }
        }

        private AppDomain CreateChildDomain(AppDomain parentDomain, string domainName)
        {
            AppDomainSetup setup = parentDomain.SetupInformation;
            setup.ShadowCopyFiles = "true";

            Evidence evidence = new Evidence(parentDomain.Evidence);
            return AppDomain.CreateDomain(domainName, evidence, setup);
        }
    }
}
