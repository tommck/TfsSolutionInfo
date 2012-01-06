using System;
using System.Diagnostics;
using System.Linq;
using EnvDTE;
using EnvDTE80;
using EnvDTE100;
using Extensibility;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.TeamFoundation.VersionControl.Common;
using Microsoft.VisualStudio.TeamFoundation.VersionControl;
using Microsoft.TeamFoundation.Client;
using System.Windows.Forms;
using System.IO;

namespace McKearney.TfsSolutionInfo
{
	/// <summary>The object for implementing an Add-in.</summary>
	/// <seealso class='IDTExtensibility2' />
	public class Connect : IDTExtensibility2
	{
        private TraceSource _trace = new TraceSource("TfsSolutionInfo");

        private DTE2 _applicationObject;
        private AddIn _addInInstance;
        private string _solutionExplorerCaption;
        private Window _solutionExplorerWindow;
        
		/// <summary>Implements the constructor for the Add-in object. Place your initialization code within this method.</summary>
		public Connect()
		{
            // wire up project loaded.
            //_trace.Switch.Level = SourceLevels.Verbose;
            _trace.TraceEvent(TraceEventType.Information, 0, "In Connect");
            //Trace.Listeners.Add(new ConsoleTraceListener(true));
		}

		/// <summary>Implements the OnConnection method of the IDTExtensibility2 interface. Receives notification that the Add-in is being loaded.</summary>
		/// <param term='application'>Root object of the host application.</param>
		/// <param term='connectMode'>Describes how the Add-in is being loaded.</param>
		/// <param term='addInInst'>Object representing this Add-in.</param>
		/// <seealso class='IDTExtensibility2' />
		public void OnConnection(object application, ext_ConnectMode connectMode, object addInInst, ref Array custom)
		{
            try
            {
                _trace.TraceEvent(TraceEventType.Information, 0, "On Connection...");
                _applicationObject = (DTE2)application;
                _addInInstance = (AddIn)addInInst;

                _applicationObject.Events.SolutionEvents.Opened += new _dispSolutionEvents_OpenedEventHandler(SolutionEvents_Opened);
                _applicationObject.Events.SolutionEvents.AfterClosing += new _dispSolutionEvents_AfterClosingEventHandler(SolutionEvents_AfterClosing);

                _applicationObject.Events.WindowEvents.WindowActivated += new _dispWindowEvents_WindowActivatedEventHandler(WindowEvents_WindowActivated);
            }
            catch (Exception ex)
            {
                _trace.TraceEvent(TraceEventType.Error, 0, "Error on Connection: " + ex.Message);
                throw;
            }
		}

        void WindowEvents_WindowActivated(Window GotFocus, Window LostFocus)
        {
            // make sure the solution explorer title is set..
            if (_solutionExplorerWindow != null && GotFocus == _solutionExplorerWindow)
            {
                if (string.IsNullOrEmpty(_solutionExplorerCaption) == false
                    && GotFocus.Caption != _solutionExplorerCaption)
                {
                    GotFocus.Caption = _solutionExplorerCaption;
                }
            }
        }

        void SolutionEvents_AfterClosing()
        {
            _trace.TraceEvent(TraceEventType.Verbose, 0, "Closed Solution");
            _solutionExplorerCaption = null;
        }

        void SolutionEvents_Opened()
        {
            try
            {
                _trace.TraceEvent(TraceEventType.Information, 0, "Solution Opened Event");

                ProcessCurrentSolution();
            }
            catch (Exception ex)
            {
                _trace.TraceEvent(TraceEventType.Error, 0, "Error on Solution Open : " + ex.Message);
                MessageBox.Show("Error on Solution Open : " + ex.Message);
                throw;
            }
        }

        private enum BranchInfoMode
        {
            NoSolution,
            TfsNotPresent,
            NotInTfs,
            InTfs,
            Error
        }

        private void ProcessCurrentSolution()
        {
            Solution4 sol = (Solution4)_applicationObject.Solution;
            BranchInfoMode mode = BranchInfoMode.NotInTfs;
            string captionInfo = string.Empty;

            try
            {
                if (sol.IsOpen == false)
                {
                    mode = BranchInfoMode.NoSolution;
                    _trace.TraceEvent(TraceEventType.Verbose, 0, "No Solution Opened");
                }
                else
                {
                    _trace.TraceEvent(TraceEventType.Information, 0, "Opened Solution: {0}", sol.FullName);

                    VersionControlExt vcExt = _applicationObject.GetObject("Microsoft.VisualStudio.TeamFoundation.VersionControl.VersionControlExt") as VersionControlExt;

                    if (vcExt == null)
                    {
                        _trace.TraceEvent(TraceEventType.Information, 0, "No TFS object");
                        mode = BranchInfoMode.TfsNotPresent;
                    }
                    else if (vcExt.SolutionWorkspace == null)
                    {
                        _trace.TraceEvent(TraceEventType.Information, 0, "No TFS Workspace for this Solution ");
                        mode = BranchInfoMode.NotInTfs;
                    }
                    else
                    {
                        VersionControlServer vcServer = vcExt.SolutionWorkspace.VersionControlServer;
                        _trace.TraceEvent(TraceEventType.Verbose, 0, "Connected to TFS Collection: {0}", vcServer.TeamProjectCollection.Name);

                        // find out where the TFS info is.
                        string serverSol = vcExt.SolutionWorkspace.GetServerItemForLocalItem(sol.FullName);
                        if (string.IsNullOrEmpty(serverSol) == true)
                        {
                            _trace.TraceEvent(TraceEventType.Warning, 0, "Solution file is NOT in TFS");
                            mode = BranchInfoMode.NotInTfs;
                        }
                        else
                        {
                            string workspaceName = vcExt.SolutionWorkspace.Name;
                            string branchLoc = GetBranchLocationForItem(vcServer, serverSol);
                            _trace.TraceEvent(TraceEventType.Information, 0, "Solution is in Branch: {0}", branchLoc);
                            captionInfo = string.Format("{0} [WS: {1}]", branchLoc, workspaceName);
                            mode = BranchInfoMode.InTfs;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                mode = BranchInfoMode.Error;
                captionInfo = ex.Message;
            }
            finally
            {
                string solName = Path.GetFileName(sol.FileName);

                // now, display that caption. if any
                switch (mode)
                {
                    case BranchInfoMode.NoSolution:
                        _solutionExplorerCaption = null; // leave it alone
                        break;
                    case BranchInfoMode.TfsNotPresent:
                        _solutionExplorerCaption = string.Format("{0} | {1}", solName, "No TFS");
                        break;
                    case BranchInfoMode.NotInTfs:
                        _solutionExplorerCaption = string.Format("{0} | {1}", solName, "Not in TFS");
                        break;
                    case BranchInfoMode.InTfs:
                    case BranchInfoMode.Error:
                        _solutionExplorerCaption = string.Format("{0} | {1}", solName, captionInfo);
                        break;
                    default:
                        throw new InvalidOperationException("Invalid value for BranchInfoMode: " + mode.ToString());
                }

                if (_solutionExplorerCaption != null)
                {
                    _trace.TraceEvent(TraceEventType.Verbose, 0, "New Window Caption: {0}", _solutionExplorerCaption);

                    _solutionExplorerWindow = _applicationObject.Windows.Item(EnvDTE.Constants.vsWindowKindSolutionExplorer);
                    _solutionExplorerWindow.Caption = _solutionExplorerCaption;
                }
            }
        }

        private static string GetBranchLocationForItem(VersionControlServer vcServer, string serverItem)
        {
            BranchObject[] rootBranches = vcServer.QueryRootBranchObjects(RecursionType.Full);
            
            string branchLocation = rootBranches
                .Where(b => 
                    {
                        var branchItem = b.Properties.RootItem.Item;
                        return VersionControlPath.GetCommonParent(branchItem, serverItem) == branchItem;
                    })
                .Select(b => b.Properties.RootItem.Item)
                .SingleOrDefault();
            return branchLocation;
        }

		/// <summary>Implements the OnDisconnection method of the IDTExtensibility2 interface. Receives notification that the Add-in is being unloaded.</summary>
		/// <param term='disconnectMode'>Describes how the Add-in is being unloaded.</param>
		/// <param term='custom'>Array of parameters that are host application specific.</param>
		/// <seealso class='IDTExtensibility2' />
        public void OnDisconnection(ext_DisconnectMode disconnectMode, ref Array custom)
        {
            try
            {
                _trace.TraceEvent(TraceEventType.Verbose, 0, "OnDisconnection");
                _applicationObject.Events.SolutionEvents.Opened -= new _dispSolutionEvents_OpenedEventHandler(SolutionEvents_Opened);
                _applicationObject.Events.SolutionEvents.AfterClosing -= new _dispSolutionEvents_AfterClosingEventHandler(SolutionEvents_AfterClosing);
            }
            catch (Exception ex)
            {
                _trace.TraceEvent(TraceEventType.Error, 0, "Error on Disconnect: " + ex.Message);
                throw;
            }
        }

		/// <summary>Implements the OnAddInsUpdate method of the IDTExtensibility2 interface. Receives notification when the collection of Add-ins has changed.</summary>
		/// <param term='custom'>Array of parameters that are host application specific.</param>
		/// <seealso class='IDTExtensibility2' />		
        public void OnAddInsUpdate(ref Array custom)
        {
            try
            {
                _trace.TraceEvent(TraceEventType.Verbose, 0, "OnAddInsUpdate");
                // if the add-in is enabled or disabled, it's updated here instead.
                ProcessCurrentSolution();
            }
            catch (Exception ex)
            {
                _trace.TraceEvent(TraceEventType.Error, 0, "Error on Update: " + ex.Message);
                throw;
            }
        }

		/// <summary>Implements the OnStartupComplete method of the IDTExtensibility2 interface. Receives notification that the host application has completed loading.</summary>
		/// <param term='custom'>Array of parameters that are host application specific.</param>
		/// <seealso class='IDTExtensibility2' />
		public void OnStartupComplete(ref Array custom)
		{
            _trace.TraceEvent(TraceEventType.Verbose, 0, "Startup Complete");
        }

		/// <summary>Implements the OnBeginShutdown method of the IDTExtensibility2 interface. Receives notification that the host application is being unloaded.</summary>
		/// <param term='custom'>Array of parameters that are host application specific.</param>
		/// <seealso class='IDTExtensibility2' />
		public void OnBeginShutdown(ref Array custom)
		{
            _trace.TraceEvent(TraceEventType.Information, 0, "Begin Shutdown");
		}
	}
}