// <copyright company="Geoinformation Winterthur">
//      Author: Edgar Butwilowski
//      Copyright (c) Geoinformation Winterthur. All rights reserved.
// </copyright>

namespace playground_check.Model;

public class DefectPicture {
    public string base64StringPicture { get; set; } = "";
    public string base64StringPictureThumb { get; set; } =  "";
    public bool afterFixing { get; set; } = false;
}