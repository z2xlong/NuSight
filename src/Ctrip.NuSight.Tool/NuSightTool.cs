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
using Microsoft.VisualStudio.ComponentModelHost;
using NuGet.VisualStudio;
using System.Windows;
using System.IO;
using SymbolBinder;
using System.Collections.Generic;
using EnvDTE80;

namespace Ctrip.NuSight.Tool
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    ///
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the 
    /// IVsPackage interface and uses the registration attributes defined in the framework to 
    /// register itself and its components with the shell.
    /// </summary>
    // This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is
    // a package.
    [PackageRegistration(UseManagedResourcesOnly = true)]
    // This attribute is used to register the information needed to show this package
    // in the Help/About dialog of Visual Studio.
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    // This attribute is needed to let the shell know that this package exposes some menus.
    [ProvideMenuResource("Menus.ctmenu", 1)]
    // This attribute registers a tool window exposed by this package.
    [ProvideAutoLoad(UIContextGuids80.SolutionExists)]
    [Guid(GuidList.guidNuSightPkgString)]
    public sealed class SlnExpShortcutMenuPackage : Package, IVsSolutionEvents
    {
        private IVsSolution _solution;
        private uint _solutionEventsCookie;
        private IVsOutputWindowPane _pane;
        private IVsPackageInstallerEvents _packageEvents;
        private IComponentModel _component;

        IComponentModel componentModel
        {
            get
            {
                if (_component == null)
                    _component = (IComponentModel)GetService(typeof(SComponentModel));
                return _component;
            }
        }

        /// <summary>
        /// Default constructor of the package.
        /// Inside this method you can place any initialization code that does not require 
        /// any Visual Studio service because at this point the package object is created but 
        /// not sited yet inside Visual Studio environment. The place to do all the other 
        /// initialization is the Initialize method.
        /// </summary>
        public SlnExpShortcutMenuPackage()
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

            //// Add our command handlers for menu (commands must exist in the .vsct file)
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

            _solution = ServiceProvider.GlobalProvider.GetService(typeof(SVsSolution)) as IVsSolution2;
            if (_solution != null)
            {
                // Register for solution events
                _solution.AdviseSolutionEvents(this, out _solutionEventsCookie);
            }
        }

        /// <summary>
        /// Executes the NuGet Visualizer.
        /// </summary>
        private void Visualize(object sender, EventArgs e)
        {
            DTE2 dte = GetService(typeof(SDTE)) as DTE2;
            var soulution = dte.Solution;
            var packageInstaller = componentModel.GetService<IVsPackageInstallerServices>();
            var visualizer = new Visualizer(packageInstaller, soulution);
            string outputFile = visualizer.CreateGraph();
            dte.ItemOperations.OpenFile(outputFile);
        }

        private void BindSymbols(object sender, EventArgs e)
        {
            IVsSolution sol = GetSolution();
            if (sol == null)
                return;

            var installerServices = componentModel.GetService<IVsPackageInstallerServices>();

            if (installerServices != null)
            {
                foreach (IVsPackageMetadata package in installerServices.GetInstalledPackages())
                {
                    BindPackage(package);
                }
            }
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
        private void SubscribeNugetEvent()
        {
            if (_packageEvents == null)
            {
                _packageEvents = componentModel.GetService<IVsPackageInstallerEvents>();
                _packageEvents.PackageReferenceAdded += packageEvents_PackageReferenceAdded;
            }
        }

        #endregion

        #region Private Utilities

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

        void packageEvents_PackageReferenceAdded(IVsPackageMetadata metadata)
        {
            BindPackage(metadata);
        }

        //this event will be not triggered while reinstall
        //private void e_PackageInstalled(IVsPackageMetadata metadata)
        //{
        //    RelocalizePackage(metadata);
        //}

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

        #endregion

        #region IVsSolutionEvents

        public int OnAfterCloseSolution(object pUnkReserved)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterOpenSolution(object pUnkReserved, int fNewSolution)
        {
            SubscribeNugetEvent();
            return VSConstants.S_OK;
        }

        public int OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeCloseSolution(object pUnkReserved)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy)
        {
            return VSConstants.S_OK;
        }

        public int OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        public int OnQueryCloseSolution(object pUnkReserved, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        public int OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        #endregion
    }
}
