// <copyright company="Geoinformation Winterthur">
//      Author: Edgar Butwilowski
//      Copyright (c) Geoinformation Winterthur. All rights reserved.
// </copyright>
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using playground_check.Configuration;

namespace playground_check.Controllers
{
    /// <summary>
    /// This is the controller for inspection type data. Inspection type data
    /// is available at the /inspection/types route.
    /// </summary>
    /// <remarks>
    /// This class provides a list of the available types of inspections.
    /// The route of this controller only provides read access, no write access.
    /// </remarks>
    [ApiController]
    [Route("/Inspection/types")]
    public class InspectionTypesController : ControllerBase
    {
        private readonly ILogger<InspectionTypesController> _logger;

        public InspectionTypesController(ILogger<InspectionTypesController> logger)
        {
            _logger = logger;
        }

        // GET inspection/types
        [HttpGet]
        [Authorize]
        public string[] GetTypes()
        {
            return InspectionTypesController._GetTypes();
        }

        internal static string[] _GetTypes()
        {
            List<string> result = new List<string>();

            using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
            {
                pgConn.Open();
                NpgsqlCommand selectComm = pgConn.CreateCommand();
                selectComm.CommandText = "SELECT short_value, value " +
                            "FROM \"wgr_sp_inspektionsart_tbd\"";

                using (NpgsqlDataReader reader = selectComm.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string shortDescription = reader.GetString(0);
                        string fullDescription = reader.GetString(1);
                        result.Add(fullDescription + " (" + shortDescription + ")");
                    }
                }
                pgConn.Close();
            }
            return result.ToArray();
        }

    }

}

