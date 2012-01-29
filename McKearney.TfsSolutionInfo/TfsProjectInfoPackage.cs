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
using EnvDTE80;
using StructureMap;
using McKearney.TfsSolutionInfo.Services;
using EnvDTE;
using EnvDTE100;
using Microsoft.TeamFoundation.VersionControl.Client;
using McKearney.TfsSolutionInfo.Helpers;
using Microsoft.VisualStudio.TeamFoundation.VersionControl;
using McKearney.TfsSolutionInfo;

namespace McKearney.TfsSolutionInfo
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
    // This attribute is used to register the informations needed to show the this package
    // in the Help/About dialog of Visual Studio.
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    // This attribute is needed to let the shell know that this package exposes some menus.
    [ProvideMenuResource("Menus.ctmenu", 1)]
    // This attribute registers a tool window exposed by this package.
    [ProvideToolWindow(typeof(MyToolWindow))]
    [Guid(GuidList.guidTfsProjectInfoPkgString)]
    public sealed class TfsProjectInfoPackage : Package
    {
        private static IVsExtensibility _extensibility;
        private static DTE2 _dte2;
        private static VersionControlExt _vcExt;
        private static SolutionEvents _solutionEvents;

        /// <summary>
        /// Default constructor of the package.
        /// Inside this method you can place any initialization code that does not require 
        /// any Visual Studio service because at this point the package object is created but 
        /// not sited yet inside Visual Studio environment. The place to do all the other 
        /// initialization is the Initialize method.
        /// </summary>
        public TfsProjectInfoPackage()
        {
            Trace.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering constructor for: {0}", this.ToString()));

            _extensibility = (IVsExtensibility)Package.GetGlobalService(typeof(IVsExtensibility));
            _dte2 = (DTE2)_extensibility.GetGlobalsObject(null).DTE;

            _vcExt = _dte2.GetObject("Microsoft.VisualStudio.TeamFoundation.VersionControl.VersionControlExt") as VersionControlExt;

            // The SolutionEvents is an *instance* variable that must be kept around for the events to work!
            _solutionEvents = _dte2.Events.SolutionEvents;
            _solutionEvents.Opened += () => ReadSolutionInfo();
            _solutionEvents.AfterClosing += () => ReadSolutionInfo();
        }

        /// <summary>
        /// This function is called when the user clicks the menu item that shows the 
        /// tool window. See the Initialize method to see how the menu item is associated to 
        /// this function using the OleMenuCommandService service and the MenuCommand class.
        /// </summary>
        private void ShowToolWindow(object sender, EventArgs e)
        {
            // Get the instance number 0 of this tool window. This window is single instance so this instance
            // is actually the only one.
            // The last flag is set to true so that if the tool window does not exists it will be created.
            ToolWindowPane window = this.FindToolWindow(typeof(MyToolWindow), 0, true);
            if ((null == window) || (null == window.Frame))
            {
                throw new NotSupportedException(Resources.CanNotCreateWindow);
            }
            IVsWindowFrame windowFrame = (IVsWindowFrame)window.Frame;
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
        }


        /////////////////////////////////////////////////////////////////////////////
        // Overriden Package Implementation
        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initilaization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            Trace.WriteLine (string.Format(CultureInfo.CurrentCulture, "Entering Initialize() of: {0}", this.ToString()));
            base.Initialize();

            // Add our command handlers for menu (commands must exist in the .vsct file)
            OleMenuCommandService mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if ( null != mcs )
            {
                // Create the command for the menu item.
                CommandID menuCommandID = new CommandID(GuidList.guidTfsProjectInfoCmdSet, (int)PkgCmdIDList.cmdidTfsProjectInfoSettings);
                MenuCommand menuItem = new MenuCommand(MenuItemCallback, menuCommandID );
                mcs.AddCommand( menuItem );

                // Create the command for the tool window
                CommandID toolwndCommandID = new CommandID(GuidList.guidTfsProjectInfoCmdSet, (int)PkgCmdIDList.cmdidTfsProjectInfoWindow);
                MenuCommand menuToolWin = new MenuCommand(ShowToolWindow, toolwndCommandID);
                mcs.AddCommand( menuToolWin );
            }

            InitializeIoC();
        }

        // TODO: Figure out how to use this
        private TraceSource _trace = new TraceSource("TfsSolutionInfo");
        private TfsInfoService _infoService = new TfsInfoService();

        private void InitializeIoC()
        {
            ObjectFactory.Initialize(x =>
                {
                    // set it to use a singleton.
                    x.For<ITfsInfoService>().Singleton().Use(_infoService);
                });

            // TODO: this may need thread dispatch stuff.
            _infoService.RefreshInfoRequested += (s, e) => { ReadSolutionInfo(); };

            ReadSolutionInfo();
        }
        private void ReadSolutionInfo()
        {
            SolutionState state = SolutionState.NoSolution;
            string workspaceName = string.Empty;
            string branchLocation = string.Empty;
            string solutionFile = string.Empty;
            string errorMessage = string.Empty;

            Solution4 sol = (Solution4)_dte2.Solution;

            try
            {
                if (sol.IsOpen == false)
                {
                    state = SolutionState.NoSolution;
                    _trace.TraceEvent(TraceEventType.Verbose, 0, "No Solution Opened");
                }
                else
                {
                    solutionFile = sol.FileName;

                    _trace.TraceEvent(TraceEventType.Information, 0, "Opened Solution: {0}", sol.FullName);

                    if (_vcExt == null)
                    {
                        _trace.TraceEvent(TraceEventType.Information, 0, "No TFS object");
                        state = SolutionState.TfsNotPresent;
                    }
                    else if (_vcExt.SolutionWorkspace == null)
                    {
                        _trace.TraceEvent(TraceEventType.Information, 0, "No TFS Workspace for this Solution ");
                        state = SolutionState.NotInTfs;
                    }
                    else
                    {
                        VersionControlServer vcServer = _vcExt.SolutionWorkspace.VersionControlServer;
                        _trace.TraceEvent(TraceEventType.Verbose, 0, "Connected to TFS Collection: {0}", vcServer.TeamProjectCollection.Name);

                        // find out where the TFS info is.
                        string serverSol = _vcExt.SolutionWorkspace.GetServerItemForLocalItem(sol.FullName);
                        if (string.IsNullOrEmpty(serverSol) == true)
                        {
                            _trace.TraceEvent(TraceEventType.Warning, 0, "Solution file is NOT in TFS");
                            state = SolutionState.NotInTfs;
                        }
                        else
                        {
                            workspaceName = _vcExt.SolutionWorkspace.Name;
                            branchLocation = TfsHelpers.GetBranchLocationForItem(vcServer, serverSol);
                            _trace.TraceEvent(TraceEventType.Information, 0, "Solution is in Branch: {0}", branchLocation);
                            state = SolutionState.InTfs;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                state = SolutionState.Error;
                _infoService.CurrentSolutionInfo = new TfsInfo(ex);
            }

            if (state != SolutionState.Error)
            {
                _infoService.CurrentSolutionInfo = new TfsInfo(state, solutionFile, branchLocation, workspaceName);
            }
        }

        #endregion

        /// <summary>
        /// This function is the callback used to execute a command when the a menu item is clicked.
        /// See the Initialize method to see how the menu item is associated to this function using
        /// the OleMenuCommandService service and the MenuCommand class.
        /// </summary>
        private void MenuItemCallback(object sender, EventArgs e)
        {
            // Show a Message Box to prove we were here
            IVsUIShell uiShell = (IVsUIShell)GetService(typeof(SVsUIShell));
            Guid clsid = Guid.Empty;
            int result;
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(uiShell.ShowMessageBox(
                       0,
                       ref clsid,
                       "TfsProjectInfo",
                       string.Format(CultureInfo.CurrentCulture, "Inside {0}.MenuItemCallback()", this.ToString()),
                       string.Empty,
                       0,
                       OLEMSGBUTTON.OLEMSGBUTTON_OK,
                       OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST,
                       OLEMSGICON.OLEMSGICON_INFO,
                       0,        // false
                       out result));
        }

    }
}
