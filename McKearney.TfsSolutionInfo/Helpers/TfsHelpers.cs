using System.Linq;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.TeamFoundation.VersionControl.Common;

namespace McKearney.TfsSolutionInfo.Helpers
{
    public static class TfsHelpers
    {
        public static string GetBranchLocationForItem(VersionControlServer vcServer, string serverItem)
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

    }
}
