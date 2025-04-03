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

    }
}
