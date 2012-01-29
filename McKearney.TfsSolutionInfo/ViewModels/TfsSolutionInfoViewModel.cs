using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using McKearney.TfsSolutionInfo.Services;
using System.IO;
using System.Windows;

namespace McKearney.TfsSolutionInfo.ViewModels
{
    class TfsSolutionInfoViewModel : ViewModel
    {
        private readonly ITfsInfoService _infoService = null;

        public TfsSolutionInfoViewModel(ITfsInfoService infoService)
        {
            if (infoService == null)
            {
                throw new ArgumentNullException("infoService");
            }

            _infoService = infoService;

            infoService.SolutionInfoChanged += (s, e) => UpdateInfo();

            // first time.
            UpdateInfo();

            this.RefreshInfoCommand = new ViewModelCommand(p => _infoService.RefreshInfo());
        }

        #region Commands
        public ViewModelCommand RefreshInfoCommand { get; private set; }
        #endregion

        private void UpdateInfo()
        {
            // TODO: error handling
            var info = _infoService.CurrentSolutionInfo;

            this.ErrorMessage = string.Empty;

            this.SolutionFile = info.SolutionFileName;
            this.SolutionName = Path.GetFileName(info.SolutionFileName);
            this.BranchLocation = info.BranchLocation;
            this.State = info.State;
            this.WorkspaceName = info.WorkspaceName;

            if (info.State == SolutionState.Error)
            {
                // TODO: more.
                this.ErrorMessage = info.Exception.Message;
            }
        }

        #region SolutionVisibility Property        
        public Visibility SolutionVisibility 
        {
            get 
            { 
                return (this.State == SolutionState.Error) ? Visibility.Collapsed : Visibility.Visible; 
            }
        }
        #endregion

        #region ErrorVisibility Property

        public Visibility ErrorVisibility
        {
            get
            {
                return (this.State == SolutionState.Error) ? Visibility.Visible : Visibility.Collapsed;
            }
        }
        #endregion

        #region ErrorMessage Property
        private string _ErrorMessage;
        public string ErrorMessage
        {
            get { return _ErrorMessage; }
            private set
            {
                if (value != _ErrorMessage)
                {
                    _ErrorMessage = value;
                    NotifyPropertyChanged("ErrorMessage");
                }
            }
        }
        #endregion

        #region SolutionName Property
        private string _SolutionName;
        public string SolutionName
        {
            get { return _SolutionName; }
            set
            {
                if (value != _SolutionName)
                {
                    _SolutionName = value;
                    NotifyPropertyChanged("SolutionName");
                }
            }
        }
        #endregion

        #region SolutionFile Property
        private string _SolutionFile;
        public string SolutionFile
        {
            get { return _SolutionFile; }
            private set
            {
                if (value != _SolutionFile)
                {
                    _SolutionFile = value;
                    NotifyPropertyChanged("SolutionFile");
                }
            }
        }
        #endregion

        #region State Property
        private SolutionState _State;
        public SolutionState State
        {
            get { return _State; }
            set
            {
                if (value != _State)
                {
                    bool wasError = _State == SolutionState.Error;

                    _State = value;
                    NotifyPropertyChanged("State");
                    NotifyPropertyChanged("StateDescription");

                    bool isError = value == SolutionState.Error;
                    if (wasError != isError)
                    {
                        NotifyPropertyChanged("SolutionVisibility");
                        NotifyPropertyChanged("ErrorVisibility");
                    }
                }
            }
        }
        #endregion

        #region StateDescription Property
        public string StateDescription
        {
            get 
            {
                string desc = "Unknown";
                switch (_State)
                {
                    case SolutionState.NoSolution:
                        desc = "No Solution";
                        break;
                    case SolutionState.TfsNotPresent:
                        desc = "TFS Is Not Installed";
                        break;
                    case SolutionState.NotInTfs:
                        desc = "Not In TFS";
                        break;
                    case SolutionState.InTfs:
                        desc = "In TFS";
                        break;
                    case SolutionState.Error:
                        desc = "Error";
                        break;
                    default:
                        throw new InvalidOperationException(string.Format("unknown Value for Solution State: {0}", State));
                }

                return desc;
            }
        }
        #endregion

        #region WorkspaceName Property
        private string _WorkspaceName;
        public string WorkspaceName
        {
            get { return _WorkspaceName; }
            set
            {
                if (value != _WorkspaceName)
                {
                    _WorkspaceName = value;
                    NotifyPropertyChanged("WorkspaceName");
                }
            }
        }
        #endregion

        #region BranchLocation Property
        private string _BranchLocation;
        public string BranchLocation
        {
            get { return _BranchLocation; }
            set
            {
                if (value != _BranchLocation)
                {
                    _BranchLocation = value;
                    NotifyPropertyChanged("BranchLocation");
                }
            }
        }
        #endregion
    }
}
