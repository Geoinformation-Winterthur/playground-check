// <copyright company="Geoinformation Winterthur">
//      Author: Edgar Butwilowski
//      Copyright (c) Geoinformation Winterthur. All rights reserved.
// </copyright>
using System.Security.Claims;
using playground_check.Controllers;
using playground_check.Model;

namespace playground_check.Services
{
    /// <summary>
    /// This is the controller for playdevice defects data. Defects data is available
    /// at the /defect route.
    /// </summary>
    /// <remarks>
    /// This class provides the possibility to store defects data in the database.
    /// </remarks>
    public class DefectService : IDefectService
    {
        private readonly ILogger<DefectService> _logger;

        public DefectService(ILogger<DefectService> logger)
        {
            _logger = logger;
        }

        public Defect Get(int tid)
        {
            DefectDAO defectDAO = new DefectDAO(); 
            return defectDAO.Read(tid);
        }

        public ErrorMessage Update(Defect[] defects,
                ClaimsPrincipal user, bool dryRun = false)
        {
            ErrorMessage result = new ErrorMessage();
            User userFromDb = LoginController.getAuthorizedUser(user, dryRun);
            if (userFromDb == null || userFromDb.fid == 0)
            {
                result.errorMessage = "Sie sind entweder nicht als Kontrolleur in der " +
                    "Spielplatzkontrolle-Datenbank erfasst oder Sie haben keine Zugriffsberechtigung.";
                return result;
            }

            if (defects != null)
            {
                try
                {
                    DefectDAO defectDao = new DefectDAO();
                    foreach (Defect defect in defects)
                    {
                        defectDao.Update(defect, userFromDb, dryRun);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message);
                    result.errorMessage = "SPK-3";
                }
            }
            else
            {
                result.errorMessage = "SPK-4";
            }
            return result;
        }

    }
}