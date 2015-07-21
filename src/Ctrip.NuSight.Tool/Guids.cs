// Guids.cs
// MUST match guids.h
using System;

namespace Ctrip.NuSight.Tool
{
    static class GuidList
    {
        public const string guidNuSightPkgString = "d0da8c10-af82-4bdd-ad08-2b28ac0457cd";
        public const string guidContextMenuSetString = "0eee2122-486c-4dcc-85b2-8f1a6688af3f";

        public static readonly Guid guidNuSightCmdSet = new Guid(guidContextMenuSetString);
    };
}