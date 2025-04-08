// <copyright company="Geoinformation Winterthur">
//      Author: Edgar Butwilowski
//      Copyright (c) Geoinformation Winterthur. All rights reserved.
// </copyright>
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

using playground_check.Model;
using playground_check.Services;

namespace playground_check.Controllers
{
    /// <summary>
    /// This is the controller for playdevice defects data. Defects data is available
    /// at the /defect route.
    /// </summary>
    /// <remarks>
    /// This class provides the possibility to store defects data in the database.
    /// </remarks>
    [ApiController]
    [Route("[controller]")]
    public class DefectController : ControllerBase
    {
        private readonly IDefectService _service;

        public DefectController(IDefectService service)
        {
            _service = service;
        }

        // POST defect/
        [HttpPost]
        [Authorize]
        public ActionResult<ErrorMessage> Post([FromBody] Defect[] defects, bool dryRun = false)
        {
            return Ok(_service.Update(defects, this.User, dryRun));
        }


        internal static void WriteAllDefects(Defect[] defects, int inspectionTid,
                     User userFromDb, bool dryRun)
        {
            if (defects != null && inspectionTid > 0 && userFromDb != null
                    && userFromDb.fid != 0)
            {
                DefectDAO defectDao = new();
                Dictionary<string, int> defectPriorityNames = defectDao.GetDefectPriorityIds();

                foreach (Defect defect in defects)
                {
                    if (defect != null && defect.defectDescription != null &&
                            defect.defectDescription.Trim().Length != 0)
                    {
                        defectPriorityNames.TryGetValue(defect.priority, out int idPriority);

                        DefectDAO.Insert(defect, idPriority, inspectionTid,
                                userFromDb, dryRun);
                    }
                }
            }
        }

    }
}