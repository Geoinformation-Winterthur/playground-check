// <copyright company="Geoinformation Winterthur">
//      Author: Edgar Butwilowski
//      Copyright (c) Geoinformation Winterthur. All rights reserved.
// </copyright>
using Npgsql;
using playground_check.Configuration;

namespace playground_check.Services
{
    public class DocumentService : IDocumentService
    {
        private readonly ILogger<DocumentService> _logger;

        public DocumentService(ILogger<DocumentService> logger)
        {
            _logger = logger;
        }

        public async Task<byte[]> GetDocumentAsync(int documentFid, string type)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(type))
                    return Array.Empty<byte>();

                type = type.Trim().ToLower();

                if (type != "abnahme" && type != "zertifikat")
                    return Array.Empty<byte>();

                await using var pgConn = new NpgsqlConnection(AppConfig.connectionString);
                await pgConn.OpenAsync();

                await using var selectPdfCommand = pgConn.CreateCommand();
                if (type == "abnahme")
                {
                    selectPdfCommand.CommandText = "SELECT abnahmedokument FROM wgr_sp_abnahmen WHERE fid = @fid";
                }
                else // zertifikat
                {
                    selectPdfCommand.CommandText = "SELECT zertifikatsdokument FROM wgr_sp_zertifikat WHERE fid = @fid";
                }
                selectPdfCommand.Parameters.AddWithValue("fid", documentFid);

                await using var reader = await selectPdfCommand.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    if (!reader.IsDBNull(0))
                        return (byte[])reader[0];
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Abrufen des Dokuments mit FID {DocumentFid}", documentFid);
            }

            return Array.Empty<byte>();
        }
    }
}
