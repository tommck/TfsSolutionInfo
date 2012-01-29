// Guids.cs
// MUST match guids.h
using System;

namespace McKearney.TfsSolutionInfo
{
    static class GuidList
    {
        public const string guidTfsProjectInfoPkgString = "7afb6672-6c20-492d-b162-a1e093637c78";
        public const string guidTfsProjectInfoCmdSetString = "ca0060cb-4a98-4a59-b89d-fe529dae1b01";
        public const string guidToolWindowPersistanceString = "bf510983-0cdc-45a0-abfc-7227d9ced9ab";

        public static readonly Guid guidTfsProjectInfoCmdSet = new Guid(guidTfsProjectInfoCmdSetString);
    };
}