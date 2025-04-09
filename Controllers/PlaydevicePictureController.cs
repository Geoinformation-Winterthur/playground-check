// <copyright company="Geoinformation Winterthur">
//      Author: Edgar Butwilowski
//      Copyright (c) Geoinformation Winterthur. All rights reserved.
// </copyright>
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using playground_check.Configuration;
using playground_check.Model;

namespace playground_check.Controllers
{
    /// <summary>
    /// This is the controller for playdevice data. Playdevice data is available
    /// at the /playdevice route.
    /// </summary>
    /// <remarks>
    /// This class provides the possibility to store playdevices data in the database.
    /// </remarks>
    [ApiController]
    [Route("Playdevice/")]
    public class PlaydevicePictureController : ControllerBase
    {

        // GET Playdevice/3736373/Picture
        [HttpGet]
        [Route("/Playdevice/{playdeviceFid}/Picture")]
        public IActionResult GetPicture(int playdeviceFid, bool dryRun = false)
        {
            try
            {
                if (dryRun) return Ok();

                byte[]? pictureData = null;
                string mimeType = "image/png"; // Fallback-MIME-Typ

                using var pgConn = new NpgsqlConnection(AppConfig.connectionString);
                pgConn.Open();

                using var cmd = pgConn.CreateCommand();
                cmd.CommandText = "SELECT picture_base64 FROM \"gr_v_spielgeraete\" WHERE fid=@fid";
                cmd.Parameters.AddWithValue("fid", playdeviceFid);

                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    if (!reader.IsDBNull(0))
                    {
                        var base64Bytes = reader.GetFieldValue<byte[]>(0);
                        var base64String = Encoding.UTF8.GetString(base64Bytes);

                        // Prüfe auf data:image/...-Prefix
                        if (base64String.StartsWith("data:"))
                        {
                            // Beispiel: data:image/jpeg;base64,/9j/4AAQSk...
                            int commaIndex = base64String.IndexOf(',');
                            if (commaIndex > 0)
                            {
                                // MIME-Typ extrahieren
                                int semicolonIndex = base64String.IndexOf(';');
                                if (semicolonIndex > 5)
                                {
                                    mimeType = base64String.Substring(5, semicolonIndex - 5); // z. B. image/jpeg
                                }

                                // Base64-Inhalt extrahieren (alles nach dem Komma)
                                string base64Content = base64String.Substring(commaIndex + 1);
                                pictureData = Convert.FromBase64String(base64Content);
                            }
                        }
                        else
                        {
                            // Falls kein Prefix: Direkt dekodieren
                            pictureData = Convert.FromBase64String(base64String);
                        }
                    }
                }

                if (pictureData == null || pictureData.Length == 0)
                {
                    return NotFound("Kein Bild vorhanden.");
                }

                return File(pictureData, mimeType);
            }
            catch (FormatException ex)
            {
                return BadRequest($"Fehler beim Dekodieren des Bildes: {ex.Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Fehler beim Laden des Bildes: {ex.Message}");
            }
        }

    }
}