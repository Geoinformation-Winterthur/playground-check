// <copyright company="Geoinformation Winterthur">
//      Author: Edgar Butwilowski
//      Copyright (c) Geoinformation Winterthur. All rights reserved.
// </copyright>
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Png;
using System.IO;
using System;

namespace playground_check.Helper;

public static class ImageHelper
{
    public static string DownsizeImage(string base64String, int maxWidth, int maxHeight)
    {
        // Entferne optionales data:image/... Prefix
        if (base64String.StartsWith("data:image"))
        {
            var commaIndex = base64String.IndexOf(',');
            base64String = base64String.Substring(commaIndex + 1);
        }

        byte[] imageBytes = Convert.FromBase64String(base64String);

        using var image = Image.Load(imageBytes);
        int originalWidth = image.Width;
        int originalHeight = image.Height;

        if (originalWidth <= maxWidth && originalHeight <= maxHeight)
            return $"data:image/png;base64,{Convert.ToBase64String(imageBytes)}";

        bool widthRules = true;
        int newHeight = (int)((maxWidth / (float)originalWidth) * originalHeight);
        if (newHeight > maxHeight)
            widthRules = false;

        int targetWidth, targetHeight;

        if (widthRules)
        {
            targetWidth = maxWidth;
            targetHeight = newHeight;
        }
        else
        {
            targetHeight = maxHeight;
            targetWidth = (int)((maxHeight / (float)originalHeight) * originalWidth);
        }

        image.Mutate(x => x.Resize(targetWidth, targetHeight));

        using var ms = new MemoryStream();
        image.Save(ms, new PngEncoder());
        string resultBase64 = Convert.ToBase64String(ms.ToArray());

        return $"data:image/png;base64,{resultBase64}";
    }

    public static string CropImage(string base64String, int ratioWidth, int ratioHeight)
    {
        if (base64String.StartsWith("data:image"))
        {
            var commaIndex = base64String.IndexOf(',');
            base64String = base64String.Substring(commaIndex + 1);
        }

        byte[] imageBytes = Convert.FromBase64String(base64String);

        using var image = Image.Load(imageBytes);
        int originalWidth = image.Width;
        int originalHeight = image.Height;

        int newHeight = (int)((originalWidth / (float)ratioWidth) * ratioHeight);

        Rectangle cropArea;

        if (newHeight > originalHeight)
        {
            int newWidth = (int)((originalHeight / (float)ratioHeight) * ratioWidth);
            int x = (originalWidth - newWidth) / 2;
            cropArea = new Rectangle(x, 0, newWidth, originalHeight);
        }
        else
        {
            int y = (originalHeight - newHeight) / 2;
            cropArea = new Rectangle(0, y, originalWidth, newHeight);
        }

        image.Mutate(x => x.Crop(cropArea));

        using var ms = new MemoryStream();
        image.Save(ms, new PngEncoder());
        string resultBase64 = Convert.ToBase64String(ms.ToArray());

        return $"data:image/png;base64,{resultBase64}";
    }

}

