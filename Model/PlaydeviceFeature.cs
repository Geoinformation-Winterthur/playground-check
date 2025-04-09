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
