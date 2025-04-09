// <copyright company="Geoinformation Winterthur">
//      Author: Edgar Butwilowski
//      Copyright (c) Geoinformation Winterthur. All rights reserved.
// </copyright>
using Microsoft.AspNetCore.Mvc;
using playground_check.Model;
using playground_check.Configuration;
using playground_check.Services;

namespace playground_check.Controllers
{
    /// <summary>
    /// This is the controller for playground data. Playground data is available
    /// at the /playground route.
    /// </summary>
    /// <remarks>
    /// This controller provides a list of the names of all playgrounds in the database.
    /// It also provides single playground objects by id and by name. It is possible to
    /// post a single playground object to this controller. Therefore, the route of this
    /// controller provides read and write access.
    /// </remarks>
    [ApiController]
    [Route("[controller]")]
    public class PlaygroundController : ControllerBase
    {
        private readonly IPlaygroundService _service;

        public PlaygroundController(IPlaygroundService service)
        {
            _service = service;
        }

        // GET /collections/playgrounds/items/
        /// <summary>
        /// Retrieves a collection of all public playgrounds of the City
        /// of Winterthur that are operated by the Municipal Green Office.
        /// </summary>
        /// <response code="200">
        /// The data is returned in an array of feature objects.
        /// </response>
        [Route("/Collections/Playgrounds/Items/")]
        [HttpGet]
        [ProducesResponseType(typeof(PlaygroundFeature[]), 200)]
        public async Task<PlaygroundFeature[]> GetFeaturesInCollection()
        {
            var result = await _service.GetFeaturesInCollection();
            return result;
        }

        // GET /collections/playgrounds/items/638364
        /// <summary>
        /// Retrieves the public playground of the City of Winterthur
        /// that is operated by the Municipal Green Office for the
        /// given UUID.
        /// </summary>
        /// <response code="200">
        /// The data is returned as a feature objects.
        /// </response>
        [Route("/Collections/Playgrounds/Items/{uuid}")]
        [HttpGet]
        [ProducesResponseType(typeof(PlaygroundFeature), 200)]
        public async Task<PlaygroundFeature> GetPlaygroundAsFeature(string uuid)
        {
            var result = await _service.GetPlaygroundAsFeature(uuid);
            return result;
        }

        // GET playground/mapimage?x=...&y=...
        [Route("/Playground/mapimage")]
        [HttpGet]
        public async Task<string> GetMapImage(double x, double y)
        {
            if (x != 0 && y != 0)
            {
                HttpClient http = new HttpClient();
                string requestUrl = "http://" + AppConfig.wmsUrl + "Spielplatzkarte?" +
                "LAYERS=AV_UEP_Landeskarten,Spielplatz&VERSION=1.1.1&DPI=96&TRANSPARENT=TRUE&FORMAT=image%2Fpng&" +
                "SERVICE=WMS&REQUEST=GetMap&STYLES=&SRS=EPSG%3A2056&BBOX=" + (x - 10) + "," + (y - 5) + "," + (x + 10) + "," +
                (y + 5) + "&WIDTH=800&HEIGHT=400";
                HttpResponseMessage resp =
                    await http.GetAsync(requestUrl);

                byte[] imageBytes = await resp.Content.ReadAsByteArrayAsync();
                string imageBase64 = Convert.ToBase64String(imageBytes);

                return imageBase64;
            }
            else
            {
                return "";
            }

        }

    }

}

