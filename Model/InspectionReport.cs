// <copyright company="Geoinformation Winterthur">
//      Author: Edgar Butwilowski
//      Copyright (c) Geoinformation Winterthur. All rights reserved.
// </copyright>

namespace playground_check.Model
{
    public class InspectionReport
    {
        public int tid { get; set; }
        public string inspectionType { get; set; } = "";
        public DateTime dateOfService{ get; set; }
        public string inspector { get; set; } = "";
        public string inspectionText { get; set; } = "";
        public bool inspectionDone { get; set; }
        public string inspectionComment { get; set; } = "";
        public string maintenanceText { get; set; } = "";
        public bool maintenanceDone { get; set; }
        public string maintenanceComment { get; set; } = "";
        public string fallProtectionType { get; set; } = "";

        public int playdeviceFid { get; set; }
        public int playdeviceDetailFid { get; set; }
        public DateTime playdeviceDateOfService { get; set; }
    }
}
