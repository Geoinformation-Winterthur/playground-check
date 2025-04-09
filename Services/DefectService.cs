// <copyright company="Geoinformation Winterthur">
//      Author: Edgar Butwilowski
//      Copyright (c) Geoinformation Winterthur. All rights reserved.
// </copyright>
using System.Security.Claims;
using System.Text;
using Npgsql;
using playground_check.Configuration;
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

        public ErrorMessage Create(Defect defect,
                ClaimsPrincipal user, bool dryRun = false)
        {
            ErrorMessage result = new ErrorMessage();
            User userFromDb = LoginController.getAuthorizedUser(user, dryRun);

            if (defect != null && userFromDb != null)
            {
                try
                {
                    DefectDAO defectDao = new DefectDAO();
                    defectDao.Insert(defect, userFromDb);
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

        public ErrorMessage Update(Defect defect,
                ClaimsPrincipal user, bool dryRun = false)
        {
            ErrorMessage result = new ErrorMessage();
            User userFromDb = LoginController.getAuthorizedUser(user, dryRun);

            if (defect != null && userFromDb != null)
            {
                try
                {
                    DefectDAO defectDao = new DefectDAO();
                    defectDao.Update(defect, userFromDb, dryRun);
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

        public async Task PutDefectPictureAsync(DefectPicture defectPic, int defectTid, bool dryRun)
        {
            if (dryRun) return;

            await using var pgConn = new NpgsqlConnection(AppConfig.connectionString);
            await pgConn.OpenAsync();

            await using var insertDefectPicCommand = pgConn.CreateCommand();
            insertDefectPicCommand.CommandText = "INSERT INTO \"wgr_sp_insp_mangel_foto\" " +
                    "(tid, tid_maengel, picture_base64, picture_base64_thumb, zeitpunkt)" +
                    "VALUES (" +
                    "(SELECT CASE WHEN max(tid) IS NULL THEN 1 ELSE max(tid) + 1 END FROM \"wgr_sp_insp_mangel_foto\"), " +
                    "@tid_maengel, @picture_base64, @picture_base64_thumb, @zeitpunkt)";

            insertDefectPicCommand.Parameters.AddWithValue("tid_maengel", defectTid);
            insertDefectPicCommand.Parameters.AddWithValue("picture_base64", defectPic.base64StringPicture);
            insertDefectPicCommand.Parameters.AddWithValue("picture_base64_thumb", defectPic.base64StringPictureThumb);
            insertDefectPicCommand.Parameters.AddWithValue("zeitpunkt", defectPic.afterFixing);
            int rowsAffected = await insertDefectPicCommand.ExecuteNonQueryAsync();

            if (rowsAffected == 0)
            {
                throw new InvalidOperationException($"Kein Mangel mit tid {defectTid} gefunden oder Bild konnte nicht gespeichert werden.");
            }
        }

    }
}