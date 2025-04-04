// <copyright company="Geoinformation Winterthur">
//      Author: Edgar Butwilowski
//      Copyright (c) Geoinformation Winterthur. All rights reserved.
// </copyright>
namespace playground_check.Model
{
    public class PlaydeviceFeature
    {
        public string type { get; set; }
        public PlaydeviceFeatureProperties properties { get; set; }
        public Geometry geometry { get; set; } = new Geometry();

        public PlaydeviceDetail[] playdeviceDetails { get; set; }
                    = new PlaydeviceDetail[0];

        public PlaydeviceFeature()
        {
            this.type = "Feature";
            this.properties = new PlaydeviceFeatureProperties();
        }

        public void switchAllCheckBoxes(bool activate)
        {

            foreach (var genInspCriterion in this.properties.generalInspectionCriteria)
            {
                InspectionReport inspectionReport = genInspCriterion.currentInspectionReport;
                if (activate)
                {
                    inspectionReport.inspectionDone = true;
                    inspectionReport.maintenanceDone = true;
                }
                else
                {
                    inspectionReport.inspectionDone = false;
                    inspectionReport.maintenanceDone = false;
                }
            }

            foreach (var mainFallInspCriterion in this.properties.mainFallProtectionInspectionCriteria)
            {
                InspectionReport inspectionReport = mainFallInspCriterion.currentInspectionReport;
                if (activate)
                {
                    inspectionReport.inspectionDone = true;
                    inspectionReport.maintenanceDone = true;
                }
                else
                {
                    inspectionReport.inspectionDone = false;
                    inspectionReport.maintenanceDone = false;
                }
            }

            foreach (var secFallInspCriterion in this.properties.secondaryFallProtectionInspectionCriteria)
            {
                InspectionReport inspectionReport = secFallInspCriterion.currentInspectionReport;
                if (activate)
                {
                    inspectionReport.inspectionDone = true;
                    inspectionReport.maintenanceDone = true;
                }
                else
                {
                    inspectionReport.inspectionDone = false;
                    inspectionReport.maintenanceDone = false;
                }
            }

            foreach (var detail in this.playdeviceDetails)
            {
                foreach (var inspectionCriterion in detail.properties.generalInspectionCriteria)
                {
                    InspectionReport inspectionReport = inspectionCriterion.currentInspectionReport;
                    if (activate)
                    {
                        inspectionReport.inspectionDone = true;
                        inspectionReport.maintenanceDone = true;
                    }
                    else
                    {
                        inspectionReport.inspectionDone = false;
                        inspectionReport.maintenanceDone = false;
                    }
                }
                foreach (var mainFallInspCriterion in detail.properties.mainFallProtectionInspectionCriteria)
                {
                    InspectionReport inspectionReport = mainFallInspCriterion.currentInspectionReport;
                    if (activate)
                    {
                        inspectionReport.inspectionDone = true;
                        inspectionReport.maintenanceDone = true;
                    }
                    else
                    {
                        inspectionReport.inspectionDone = false;
                        inspectionReport.maintenanceDone = false;
                    }
                }

                foreach (var secFallInspCriterion in detail.properties.secondaryFallProtectionInspectionCriteria)
                {
                    InspectionReport inspectionReport = secFallInspCriterion.currentInspectionReport;
                    if (activate)
                    {
                        inspectionReport.inspectionDone = true;
                        inspectionReport.maintenanceDone = true;
                    }
                    else
                    {
                        inspectionReport.inspectionDone = false;
                        inspectionReport.maintenanceDone = false;
                    }
                }
            }
            this.evaluateChecks();
        }


        /**
         * This method tests if this this and its this-
         * details have any inspection criteria ("checks") on them
         * and stores the result in the "hasChecks" attribute of the
         * given playground.
         * 
         * @param this The this object to check for
         */
        public void evaluateHasChecks()
        {
            this.properties.hasChecks = false;
            if (this.properties.generalInspectionCriteria.Length != 0
                || this.properties.mainFallProtectionInspectionCriteria.Length != 0
                || this.properties.secondaryFallProtectionInspectionCriteria.Length != 0)
            {
                this.properties.hasChecks = true;
                return;
            }
            foreach (var thisDetail in this.playdeviceDetails)
            {
                bool hasDetailChecks = thisDetail.evaluateHasChecks();
                this.properties.hasChecks = hasDetailChecks;
            }
        }

        /**
         * This method merges the status of all inspections ("checks")
         * to one value "hasOpenChecks" of the given this.
         * 
         * @param this The this object to check for
         */
        public void evaluateChecks()
        {
            this.properties.hasOpenChecks = false;

            // check if given this has checks (inspection criteria):
            this.evaluateHasChecks();

            bool hasDetailOpenChecks = false;
            foreach (var thisDetail in this.playdeviceDetails)
            {
                hasDetailOpenChecks = thisDetail.evaluateChecks();
                if (!this.properties.hasOpenChecks)
                {
                    this.properties.hasOpenChecks = hasDetailOpenChecks;
                }
            }

            if (this.properties.hasChecks)
            {
                foreach (var inspectionCreterion in this.properties.generalInspectionCriteria)
                {
                    if (!inspectionCreterion.currentInspectionReport.inspectionDone ||
                      !inspectionCreterion.currentInspectionReport.maintenanceDone)
                    {
                        this.properties.hasOpenChecks = true;
                    }
                }

                foreach (var inspectionCreterion in this.properties.mainFallProtectionInspectionCriteria)
                {
                    if (!inspectionCreterion.currentInspectionReport.inspectionDone ||
                      !inspectionCreterion.currentInspectionReport.maintenanceDone)
                    {
                        this.properties.hasOpenChecks = true;
                    }
                }

                foreach (var inspectionCreterion in this.properties.secondaryFallProtectionInspectionCriteria)
                {
                    if (!inspectionCreterion.currentInspectionReport.inspectionDone ||
                      !inspectionCreterion.currentInspectionReport.maintenanceDone)
                    {
                        this.properties.hasOpenChecks = true;
                    }
                }

            }


        }

    }
}
