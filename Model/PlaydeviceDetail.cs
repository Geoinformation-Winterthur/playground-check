// <copyright company="Geoinformation Winterthur">
//      Author: Edgar Butwilowski
//      Copyright (c) Geoinformation Winterthur. All rights reserved.
// </copyright>
namespace playground_check.Model
{
    public class PlaydeviceDetail
    {
        public PlaydeviceFeatureProperties properties { get; set; }
                    = new PlaydeviceFeatureProperties();

        public bool evaluateHasChecks()
        {
            this.properties.hasChecks = false;
            if (this.properties.generalInspectionCriteria.Length != 0
                || this.properties.mainFallProtectionInspectionCriteria.Length != 0
                || this.properties.secondaryFallProtectionInspectionCriteria.Length != 0)
            {
                this.properties.hasChecks = true;
            }
            return this.properties.hasChecks;
        }

        /**
         * This method tests if this playdevice detail has any "old" inspection
         * reports (last and before last) on it and stores the result in the
         * "hasOldReports" attribute of the given playdevice detail.
         *
         * @param this The playdevice detail object to check for
         * @returns
         */
        public bool evaluateHasOldInspectionReports()
        {
            this.properties.hasOldReports = false;
            if ((this.properties.lastInspectionReports != null &&
                    this.properties.lastInspectionReports.Length != 0) ||
                    (this.properties.nextToLastInspectionReports != null &&
                            this.properties.nextToLastInspectionReports.Length != 0))
            {
                this.properties.hasOldReports = true;
            }
            return this.properties.hasOldReports;
        }

        public bool evaluateChecks()
        {
            this.properties.hasOpenChecks = false;
            foreach (var inspectionCreterion in this.properties.generalInspectionCriteria)
            {
                if (inspectionCreterion.currentInspectionReport != null)
                {
                    if (!inspectionCreterion.currentInspectionReport.inspectionDone ||
                            !inspectionCreterion.currentInspectionReport.maintenanceDone)
                    {
                        this.properties.hasOpenChecks = true;
                        return true;
                    }
                }
            }
            foreach (var inspectionCreterion in this.properties.mainFallProtectionInspectionCriteria)
            {
                if (inspectionCreterion.currentInspectionReport != null)
                {
                    if (!inspectionCreterion.currentInspectionReport.inspectionDone ||
                            !inspectionCreterion.currentInspectionReport.maintenanceDone)
                    {
                        this.properties.hasOpenChecks = true;
                        return true;
                    }
                }
            }
            foreach (var inspectionCreterion in this.properties.secondaryFallProtectionInspectionCriteria)
            {
                if (inspectionCreterion.currentInspectionReport != null)
                {
                    if (!inspectionCreterion.currentInspectionReport.inspectionDone ||
                            !inspectionCreterion.currentInspectionReport.maintenanceDone)
                    {
                        this.properties.hasOpenChecks = true;
                        return true;
                    }
                }
            }
            return false;
        }

        public bool evaluateSomeOldDefectsAreDone()
        {

            this.properties.someOldDefectsAreDone = false;

            if (this.properties.defects != null)
            {
                foreach (var defect in this.properties.defects)
                {
                    if (defect.dateDone != null)
                    {
                        this.properties.someOldDefectsAreDone = true;
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
