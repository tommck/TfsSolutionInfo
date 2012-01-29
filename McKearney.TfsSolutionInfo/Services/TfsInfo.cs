
using System;
namespace McKearney.TfsSolutionInfo.Services
{
    class TfsInfo
    {
        public TfsInfo(Exception exception)
        {
            this.State = SolutionState.Error;
            this.Exception = exception;
        }

        public TfsInfo(SolutionState state, string sol, string branch, string workspace)
        {
            if (state == SolutionState.Error)
            {
                throw new ArgumentOutOfRangeException("state", "A SolutionState of Error requires passing in the Exception");
            }

            this.State = state;
            this.SolutionFileName = sol;
            this.BranchLocation = branch;
            this.WorkspaceName = workspace;
        }
        public SolutionState State { get; set; }
        public string SolutionFileName { get; private set; }
        public string BranchLocation { get; private set; }
        public string WorkspaceName { get; private set; }
        public Exception Exception { get; private set; }
    }
}
