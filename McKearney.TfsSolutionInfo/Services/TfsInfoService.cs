using System;

namespace McKearney.TfsSolutionInfo.Services
{
    class TfsInfoService : ITfsInfoService
    {
        public TfsInfoService()
        {
        }

        #region ITfsInfoService Members

        private TfsInfo _currentInfo;
        public TfsInfo CurrentSolutionInfo
        {
            get { return _currentInfo; }
            set 
            { 
                _currentInfo = value;
                SolutionInfoChanged(this, EventArgs.Empty);
            }
        }

        public event EventHandler SolutionInfoChanged = delegate { };

        public event EventHandler RefreshInfoRequested = delegate { };

        public void RefreshInfo()
        {
            RefreshInfoRequested(this, EventArgs.Empty);
        }

        #endregion
    }
}
