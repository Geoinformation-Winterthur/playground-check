// <copyright company="Geoinformation Winterthur">
//      Author: Edgar Butwilowski
//      Copyright (c) Geoinformation Winterthur. All rights reserved.
// </copyright>
namespace playground_check.Model
{
    public class PlaygroundFeature
    {
        public string type { get; set; }
        public PlaygroundFeatureProperties properties { get; set; }
        public PlaygroundFeaturePoint geometry { get; set; } = new PlaygroundFeaturePoint();
        public string errorMessage { get; set; } = "";

        public PlaygroundFeature()
        {
            this.type = "Feature";
            this.properties = new PlaygroundFeatureProperties();
        }
    }
}
