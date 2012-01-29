using System;

namespace McKearney.TfsSolutionInfo.Services
{
    internal interface ITfsInfoService
    {
        /// <summary>
        /// Refreshes the info.
        /// </summary>
        void RefreshInfo();

        /// <summary>
        /// Gets the current solution info.
        /// </summary>
        TfsInfo CurrentSolutionInfo { get; }

        /// <summary>
        /// Occurs when [solution info changed].
        /// </summary>
        event EventHandler SolutionInfoChanged;

        /// <summary>
        /// Occurs when [refresh info requested].
        /// </summary>
        event EventHandler RefreshInfoRequested;
    }
}
