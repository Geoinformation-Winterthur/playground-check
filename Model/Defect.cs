// <copyright company="Geoinformation Winterthur">
//      Author: Edgar Butwilowski
//      Copyright (c) Geoinformation Winterthur. All rights reserved.
// </copyright>
namespace playground_check.Model
{
    public class Defect
    {
        public int tid { get; set; }
        public int playdeviceFid { get; set; }
        public int priority { get; set; }
        public bool done = false;
        public string defectDescription { get; set; } = "";
        public DateTime? dateCreation { get; set; }
        public DateTime? dateDone { get; set; }
        public string defectComment { get; set; } = "";
        public int defectsResponsibleBodyId { get; set; } = -1;
        public DefectPicture[] pictures { get; set;} = Array.Empty<DefectPicture>();
    }
}
