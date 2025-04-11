using System.Text;
using Npgsql;
using playground_check.Configuration;

namespace playground_check.Services;

/// <summary>
/// This is the service for playground data. Playground data is available
/// at the /playground route.
/// </summary>
/// <remarks>
/// This service provides a list of the names of all playgrounds in the database.
/// It also provides single playground objects by id and by name. It is possible to
/// post a single playground object to this service. Therefore, the route of this
/// service provides read and write access.
/// </remarks>
public class PlaydeviceService : IPlaydeviceService
{
    private readonly ILogger<PlaydeviceService> _logger;

    public PlaydeviceService(ILogger<PlaydeviceService> logger)
    {
        _logger = logger;
    }

    public async Task<string> GetPictureAsync(int playdeviceFid, bool dryRun = false)
    {
        if (dryRun) return "";

        await using var pgConn = new NpgsqlConnection(AppConfig.connectionString);
        await pgConn.OpenAsync();

        await using var cmd = pgConn.CreateCommand();
        cmd.CommandText = "SELECT picture_base64 FROM \"gr_v_spielgeraete\" WHERE fid = @fid";
        cmd.Parameters.AddWithValue("fid", playdeviceFid);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            if (!reader.IsDBNull(0))
            {
                byte[] pictureBytes = (byte[])reader[0];

                // Bild liegt als kompletter data:image/... String vor
                return Encoding.UTF8.GetString(pictureBytes);
            }
        }

        return "";
    }


    public async Task PutPictureAsync(int playdeviceFid, string pictureBase64String, bool dryRun)
    {
        if (dryRun) return;

        await using var pgConn = new NpgsqlConnection(AppConfig.connectionString);
        Task openPgConnTask = pgConn.OpenAsync();

        // Base64-String in Byte-Array umwandeln
        byte[] pictureBytes = Encoding.UTF8.GetBytes(pictureBase64String);

        await openPgConnTask;
        await using var updatePictureCommand = pgConn.CreateCommand();
        updatePictureCommand.CommandText = "UPDATE \"gr_v_spielgeraete\" " +
                                           "SET picture_base64 = @picture_base64 " +
                                           "WHERE fid = @fid";
        updatePictureCommand.Parameters.AddWithValue("fid", playdeviceFid);
        updatePictureCommand.Parameters.AddWithValue("picture_base64", pictureBytes);
        int rowsAffected = await updatePictureCommand.ExecuteNonQueryAsync();

        if (rowsAffected == 0)
        {
            throw new InvalidOperationException($"Kein Spielgerät mit fid {playdeviceFid} gefunden oder Bild konnte nicht gespeichert werden.");
        }
    }
}