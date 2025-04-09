// <copyright company="Geoinformation Winterthur">
//      Author: Edgar Butwilowski
//      Copyright (c) Geoinformation Winterthur. All rights reserved.
// </copyright>
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using NpgsqlTypes;
using playground_check.Configuration;
using playground_check.Model;

namespace playground_check.Controllers
{
    /// <summary>
    /// This is the controller for inspection data. Inspection data is available
    /// at the /inspection route.
    /// </summary>
    /// <remarks>
    /// This class provides a list of the available types of inspections, as well
    /// as the inspection reports data itself. The route of this controller only
    /// provides read access, no write access.
    /// </remarks>
    [ApiController]
    [Route("[controller]")]
    public class InspectionController : ControllerBase
    {
        private readonly ILogger<InspectionController> _logger;

        public InspectionController(ILogger<InspectionController> logger)
        {
            _logger = logger;
        }

        private static int writeInspection(InspectionReport exampleInspectionReport,
                    User userFromDb, NpgsqlConnection pgConn, bool dryRun)
        {
            int inspectorFid = -1;
            int inspectionTypeId = -1;

            NpgsqlCommand selectInspectorFid;
            selectInspectorFid = pgConn.CreateCommand();
            selectInspectorFid.CommandText = "SELECT fid FROM \"wgr_sp_kontrolleur\" " +
                        "WHERE e_mail = @e_mail";
            selectInspectorFid.Parameters.AddWithValue("e_mail", userFromDb.mailAddress);

            using (NpgsqlDataReader reader = selectInspectorFid.ExecuteReader())
            {
                if (reader.Read())
                {
                    inspectorFid = reader.GetInt32(0);
                }
            }

            string inspectionType = exampleInspectionReport.inspectionType;
            if (exampleInspectionReport.inspectionType.Length > 4)
            {
                inspectionType = exampleInspectionReport.inspectionType
                        .Substring(0, exampleInspectionReport.inspectionType.Length - 5);
            }

            NpgsqlCommand selectInspectionTypeId;
            selectInspectionTypeId = pgConn.CreateCommand();
            selectInspectionTypeId.CommandText = "SELECT id FROM \"wgr_sp_inspektionsart_tbd\" " +
                        "WHERE value = @value";
            selectInspectionTypeId.Parameters.AddWithValue("value", inspectionType);

            using (NpgsqlDataReader reader = selectInspectionTypeId.ExecuteReader())
            {
                if (reader.Read())
                {
                    inspectionTypeId = reader.GetInt32(0);
                }
            }

            int playgroundFid = _GetPlaygroundFid(exampleInspectionReport, pgConn);

            NpgsqlDate? targetDateOfInspection = _GetTargetDateOfInspection(playgroundFid, inspectionTypeId, pgConn);

            if (dryRun) return -1;

            NpgsqlCommand insertInspectionCommand;
            insertInspectionCommand = pgConn.CreateCommand();
            insertInspectionCommand.CommandText = "INSERT INTO \"wgr_sp_inspektion\" " +
                    "(tid, id_inspektionsart, fid_spielplatz, datum_inspektion, fid_kontrolleur, " +
                    "datum_soll_inspektion) " +
                    "VALUES (" +
                    "(SELECT CASE WHEN max(tid) IS NULL THEN 1 ELSE max(tid) + 1 END FROM \"wgr_sp_inspektion\"), " +
                    "@id_inspektionsart, @fid_spielplatz, @datum_inspektion, @fid_kontrolleur, @datum_soll_inspektion) " +
                    "RETURNING tid";
            insertInspectionCommand.Parameters.AddWithValue("id_inspektionsart",
                        inspectionTypeId != -1 ? inspectionTypeId : DBNull.Value);
            insertInspectionCommand.Parameters.AddWithValue("fid_spielplatz",
                        playgroundFid != -1 ? playgroundFid : DBNull.Value);
            NpgsqlDate dateOfService = (NpgsqlDate)exampleInspectionReport.playdeviceDateOfService;
            insertInspectionCommand.Parameters.AddWithValue("datum_inspektion", dateOfService);
            insertInspectionCommand.Parameters.AddWithValue("fid_kontrolleur",
                        inspectorFid != -1 ? inspectorFid : DBNull.Value);
            insertInspectionCommand.Parameters.AddWithValue("datum_soll_inspektion",
                        targetDateOfInspection != null ? targetDateOfInspection : DBNull.Value);
            int inspectionTid = (int)insertInspectionCommand.ExecuteScalar();

            return inspectionTid;
        }

        private static int WriteInspectionReport(int inspectionTid, InspectionReport inspectionReport,
                    User userFromDb, NpgsqlConnection pgConn, bool dryRun)
        {
            if (dryRun) return -1;

            NpgsqlCommand insertInspectionReportCommand;
            insertInspectionReportCommand = pgConn.CreateCommand();
            insertInspectionReportCommand.CommandText = "INSERT INTO \"wgr_sp_insp_bericht\" " +
                    "(tid, tid_inspektion, fid_spielgeraet, fid_geraet_detail, inspektionsart, datum_inspektion, kontrolleur, pruefung_text, " +
                    "pruefung_erledigt, pruefung_kommentar, wartung_text, wartung_erledigung, " +
                    "wartung_kommentar, fallschutz) " +
                    "VALUES (" +
                    "(SELECT CASE WHEN max(tid) IS NULL THEN 1 ELSE max(tid) + 1 END FROM \"wgr_sp_insp_bericht\"), " +
                    "@tid_inspektion, @fid_spielgeraet, @fid_geraet_detail, @inspektionsart, @datum_inspektion, @kontrolleur, @pruefung_text, " +
                    "@pruefung_erledigt, @pruefung_kommentar, @wartung_text, @wartung_erledigung, " +
                    "@wartung_kommentar, @fallschutz) RETURNING tid";
            insertInspectionReportCommand.Parameters.AddWithValue("tid_inspektion",
                inspectionTid != -1 ? inspectionTid : DBNull.Value);
            insertInspectionReportCommand.Parameters.AddWithValue("fid_spielgeraet",
                            inspectionReport.playdeviceFid != 0 ? inspectionReport.playdeviceFid : DBNull.Value);
            insertInspectionReportCommand.Parameters.AddWithValue("fid_geraet_detail",
                            inspectionReport.playdeviceDetailFid != 0 ? inspectionReport.playdeviceDetailFid : DBNull.Value);
            insertInspectionReportCommand.Parameters.AddWithValue("inspektionsart", inspectionReport.inspectionType);
            NpgsqlDate dateOfService = (NpgsqlDate)inspectionReport.playdeviceDateOfService;
            insertInspectionReportCommand.Parameters.AddWithValue("datum_inspektion", dateOfService);
            insertInspectionReportCommand.Parameters.AddWithValue("kontrolleur", userFromDb.firstName + " " + userFromDb.lastName);
            if (inspectionReport.inspectionText == null) inspectionReport.inspectionText = "";
            insertInspectionReportCommand.Parameters.AddWithValue("pruefung_text", inspectionReport.inspectionText);
            int inspectionDone = inspectionReport.inspectionDone ? 1 : 0;
            insertInspectionReportCommand.Parameters.AddWithValue("pruefung_erledigt", inspectionDone);
            if (inspectionReport.inspectionComment == null || inspectionReport.inspectionComment.Trim().Length == 0)
            {
                inspectionReport.inspectionComment = "";
            }
            insertInspectionReportCommand.Parameters.AddWithValue("pruefung_kommentar", inspectionReport.inspectionComment);
            if (inspectionReport.maintenanceText == null) inspectionReport.maintenanceText = "";
            insertInspectionReportCommand.Parameters.AddWithValue("wartung_text", inspectionReport.maintenanceText);
            int maintenanceDone = inspectionReport.maintenanceDone ? 1 : 0;
            insertInspectionReportCommand.Parameters.AddWithValue("wartung_erledigung", maintenanceDone);
            if (inspectionReport.maintenanceComment == null || inspectionReport.maintenanceComment.Trim().Length == 0)
            {
                inspectionReport.maintenanceComment = "";
            }
            insertInspectionReportCommand.Parameters.AddWithValue("wartung_kommentar", inspectionReport.maintenanceComment);
            insertInspectionReportCommand.Parameters.AddWithValue("fallschutz", inspectionReport.fallProtectionType);

            return (int)insertInspectionReportCommand.ExecuteScalar();

        }

        private static int _GetPlaygroundFid(InspectionReport exampleInspectionReport, NpgsqlConnection pgConn)
        {
            int result = -1;

            int playdeviceFid = exampleInspectionReport.playdeviceFid;

            if (exampleInspectionReport.playdeviceFid == 0 &&
                        exampleInspectionReport.playdeviceDetailFid != 0)
            {
                NpgsqlCommand selectPlaydeviceFid;
                selectPlaydeviceFid = pgConn.CreateCommand();
                selectPlaydeviceFid.CommandText = "SELECT fid_spielgeraet FROM \"wgr_sp_geraetedetail\" " +
                            "WHERE fid = @fid_spielgeraetedetail";
                selectPlaydeviceFid.Parameters
                        .AddWithValue("fid_spielgeraetedetail", exampleInspectionReport.playdeviceDetailFid);

                using (NpgsqlDataReader reader = selectPlaydeviceFid.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        playdeviceFid = reader.GetInt32(0);
                    }
                }

            }

            if (playdeviceFid != 0)
            {
                NpgsqlCommand selectPlaygoundFid;
                selectPlaygoundFid = pgConn.CreateCommand();
                selectPlaygoundFid.CommandText = "SELECT fid_spielplatz FROM \"gr_v_spielgeraete\" " +
                            "WHERE fid = @fid_spielgeraet";
                selectPlaygoundFid.Parameters.AddWithValue("fid_spielgeraet", playdeviceFid);

                using (NpgsqlDataReader reader = selectPlaygoundFid.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        result = reader.GetInt32(0);
                    }
                }
            }
            return result;
        }

        private static NpgsqlDate? _GetTargetDateOfInspection(int playgroundFid, int inspectionTypeId, NpgsqlConnection pgConn)
        {

            if(inspectionTypeId < 1 || inspectionTypeId > 3) return null;

            NpgsqlDate? result = null;

            string inspectionAttrName = "dat_naech_visu_insp";
            switch (inspectionTypeId)
            {
                case 2: inspectionAttrName = "dat_naech_oper_insp";break;
                case 3: inspectionAttrName = "dat_naech_haupt_insp";break;
            }

            NpgsqlCommand selectTargetDateComm;
            selectTargetDateComm = pgConn.CreateCommand();
            selectTargetDateComm.CommandText = "SELECT " + inspectionAttrName +
                        " FROM \"wgr_sp_spielplatz\"" +
                        " WHERE fid = @playground_fid";
            selectTargetDateComm.Parameters
                    .AddWithValue("playground_fid", playgroundFid);

            using (NpgsqlDataReader reader = selectTargetDateComm.ExecuteReader())
            {
                if (reader.Read() && !reader.IsDBNull(0))
                {
                    result = reader.GetDate(0);
                }
            }

            return result;
        }


    }

}

