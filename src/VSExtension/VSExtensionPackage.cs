using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.ComponentModel.Design;
using Microsoft.Win32;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using SymbolBinder;
using NuGet.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;

namespace NuSight.VSExtension
{
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideAutoLoad(UIContextGuids80.SolutionExists)]
    [Guid(GuidList.guidNuSightPkgString)]
    public sealed class VSExtensionPackage : Package
    {
        private IVsOutputWindowPane _pane;
        private IVsSolution _solution;

        public VSExtensionPackage()
        {
            Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering constructor for: {0}", this.ToString()));
        }

        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering Initialize() of: {0}", this.ToString()));
            base.Initialize();

            // Add our command handlers for menu (commands must exist in the .vsct file)
            OleMenuCommandService mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (null != mcs)
            {
                CommandID slncmd = new CommandID(GuidList.guidNuSightCmdSet, (int)PkgCmdIDList.cmdBindSymbol);
                MenuCommand mBind = new MenuCommand(BindSymbols, slncmd);
                mcs.AddCommand(mBind);

                CommandID vslcmd = new CommandID(GuidList.guidNuSightCmdSet, (int)PkgCmdIDList.cmdVisualize);
                MenuCommand mVisual = new MenuCommand(Visualize, vslcmd);
                mcs.AddCommand(mVisual);
            }
        }
        #endregion

        private void Visualize(object sender, EventArgs e)
        {
            //var componentModel = (IComponentModel)GetService(typeof(SComponentModel));
            //var installerServices = componentModel.GetService<IVsPackageManagerFactory>();
            var s = ServiceLocator.GetInstance<IVsPackageManagerFactory>();
            PrintToOutputWindow(s.GetType().ToString());
            //var visualizer = new Visualizer(
            //ServiceLocator.GetInstance<IVsPackageManagerFactory>(),
            //ServiceLocator.GetInstance<ISolutionManager>());
            //string outputFile = visualizer.CreateGraph();

        }


        private void BindSymbols(object sender, EventArgs e)
        {
            //IVsSolution sol = GetSolution();
            //if (sol == null)
            //    return;

            //var componentModel = (IComponentModel)GetService(typeof(SComponentModel));
            //var installerServices = componentModel.GetService<IVsPackageInstallerServices>();

            //if (installerServices != null)
            //{
            //    foreach (IVsPackageMetadata package in installerServices.GetInstalledPackages())
            //    {
            //        BindPackage(package);
            //    }
            //}
        }

        private void BindPackage(IVsPackageMetadata package)
        {
            try
            {
                if (Binder.Execute(package.InstallPath))
                    PrintToOutputWindow(string.Format("{0}{1} locaolized Complete.\r\n", package.Id, package.VersionString));
            }
            catch (Exception ex)
            {
                PrintToOutputWindow(string.Format("{0}{1} locaolized fail: {2}\r\n", package.Id, package.VersionString, ex.Message));
            }
        }

        private void PrintToOutputWindow(string message)
        {
            if (_pane == null)
                CreatePane();

            if (_pane != null)
            {
                _pane.Activate();
                _pane.OutputString(message);
            }
        }

        private void CreatePane()
        {
            if (_pane != null)
                return;

            Guid paneGuid = new Guid("0A3A19C1-FE4A-4A49-988C-0CC8E478203A");
            IVsOutputWindow output = (IVsOutputWindow)GetService(typeof(SVsOutputWindow));
            output.CreatePane(ref paneGuid, "Package Binder", Convert.ToInt32(true), Convert.ToInt32(true));
            output.GetPane(ref paneGuid, out _pane);
        }

        private IVsSolution GetSolution()
        {
            IVsSolution sol = (IVsSolution)GetService(typeof(SVsSolution));
            return sol;
        }

        private IVsHierarchy GetSelectedHierarchy()
        {
            IVsMonitorSelection monitorSelection = this.GetService(typeof(IVsMonitorSelection)) as IVsMonitorSelection;
            if (monitorSelection == null)
                return null;

            IntPtr hierarchyPtr = IntPtr.Zero;
            IntPtr selectionContainer = IntPtr.Zero;
            IVsMultiItemSelect mis;
            uint prjItemId;

            try
            {
                monitorSelection.GetCurrentSelection(out hierarchyPtr, out prjItemId, out mis, out selectionContainer);
                IVsHierarchy selectedHierarchy = Marshal.GetTypedObjectForIUnknown(hierarchyPtr, typeof(IVsHierarchy)) as IVsHierarchy;
                return selectedHierarchy;
            }
            catch
            {
                return null;
            }
        }

    }
}
