// <copyright company="Geoinformation Winterthur">
//      Author: Edgar Butwilowski
//      Copyright (c) Geoinformation Winterthur. All rights reserved.
// </copyright>
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using playground_check.Model;
using playground_check.Configuration;
using playground_check.Helper;
using System.Security.Claims;
using System.Net.Mail;

namespace playground_check.Service;

public class UserService : IUserService
{
    private readonly ILogger<UserService> _logger;

    public UserService(ILogger<UserService> logger)
    {
        _logger = logger;
    }

    public User GetUser(string email)
    {
        return this.GetUsers(email).First<User>();
    }

    public User[] GetUsers(string? email)
    {
        List<User> usersFromDb = new List<User>();
        // get data of current user from database:
        using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
        {
            pgConn.Open();
            NpgsqlCommand selectComm = pgConn.CreateCommand();
            selectComm.CommandText = @"SELECT nachname, vorname,
                                    trim(lower(e_mail)), aktiv, rolle, is_new
                                FROM ""wgr_sp_kontrolleur""";

            if (email != null)
            {
                email = email.ToLower().Trim();
            }

            if (email != null && email != "")
            {
                selectComm.CommandText += " WHERE trim(lower(e_mail))=@email";
                selectComm.Parameters.AddWithValue("email", email);
            }
            selectComm.CommandText += " ORDER BY vorname, nachname";

            using (NpgsqlDataReader reader = selectComm.ExecuteReader())
            {
                User userFromDb;
                while (reader.Read())
                {
                    userFromDb = new User();
                    userFromDb.mailAddress =
                            reader.IsDBNull(2) ? "" :
                                    reader.GetString(2).ToLower().Trim();
                    if (userFromDb.mailAddress != null && userFromDb.mailAddress != "")
                    {
                        userFromDb.lastName = reader.IsDBNull(0) ? "" : reader.GetString(0);
                        userFromDb.lastName = userFromDb.lastName.Trim();
                        userFromDb.firstName = reader.IsDBNull(1) ? "" : reader.GetString(1);
                        userFromDb.firstName = userFromDb.firstName.Trim();

                        userFromDb.active = reader.IsDBNull(3) ? false : reader.GetBoolean(3);
                        userFromDb.role = reader.IsDBNull(4) ? "" : reader.GetString(4);
                        userFromDb.isNew = reader.IsDBNull(5) ? false : reader.GetBoolean(5);

                        usersFromDb.Add(userFromDb);
                    }
                }
            }
            pgConn.Close();
        }

        return usersFromDb.ToArray<User>();
    }

    public User UpdateUser(User user, bool changePassphrase = false)
    {
        User result = new User();
        try
        {
            string userPassphrase = user.passPhrase;
            user.passPhrase = "";
            if (user == null)
            {
                _logger.LogInformation("No user data provided by user in update user process.");
                result.errorMessage = "SPK-3";
                return result;
            }

            if (user.mailAddress == null)
            {
                user.mailAddress = "";
            }
            user.mailAddress = user.mailAddress.ToLower().Trim();

            if (user.mailAddress == "")
            {
                _logger.LogWarning("No user data provided by user in update user process.");
                result.errorMessage = "SPK-3";
                return result;
            }

            try
            {
                MailAddress userMailAddress = new MailAddress(user.mailAddress);
            }
            catch (Exception ex)
            {
                _logger.LogInformation(ex.Message);
                result.errorMessage = "SPK-3";
                return result;
            }

            User userInDb = new User();
            ActionResult<User[]> usersInDbResult = this.GetUsers(user.mailAddress);
            User[]? usersInDb = usersInDbResult.Value;
            if (usersInDb == null || usersInDb.Length != 1 || usersInDb[0] == null)
            {
                _logger.LogWarning("Updating user " + user.mailAddress + " is not possible since user is not in the database.");
                result.errorMessage = "SPK-3";
                return result;
            }

            userInDb = usersInDb[0];
            if (userInDb.role == "administrator")
            {
                int noOfActiveAdmins = _countNumberOfActiveAdmins();
                if (noOfActiveAdmins == 1)
                {
                    if (user.role != "administrator")
                    {
                        _logger.LogWarning("Administrator tried to change role of last administrator. " +
                                "Role cannot be changed since there would be no administrator anymore.");
                        result.errorMessage = "SPK-3";
                        return result;
                    }

                    if (!user.active)
                    {
                        _logger.LogWarning("Administrator tried to set last administrator inactive. " +
                                "This is not allowed.");
                        result.errorMessage = "SPK-3";
                        return result;
                    }
                }
            }

            using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
            {
                pgConn.Open();
                NpgsqlCommand updateComm = pgConn.CreateCommand();
                updateComm.CommandText = @"UPDATE ""wgr_sp_kontrolleur""
                            SET nachname=@last_name, vorname=@first_name,
                            rolle=@role, aktiv=@active, is_new=@is_new
                            WHERE e_mail=@e_mail";

                updateComm.Parameters.AddWithValue("last_name", user.lastName);
                updateComm.Parameters.AddWithValue("first_name", user.firstName);
                updateComm.Parameters.AddWithValue("role", user.role);
                updateComm.Parameters.AddWithValue("active", user.active);
                updateComm.Parameters.AddWithValue("e_mail", user.mailAddress);
                updateComm.Parameters.AddWithValue("is_new", user.isNew);

                int noAffectedRowsStep1 = updateComm.ExecuteNonQuery();

                if (changePassphrase == true)
                {
                    userPassphrase = userPassphrase.Trim();
                    if (userPassphrase.Length < 8)
                    {
                        _logger.LogWarning("Not enough user data provided in update user process.");
                        result.errorMessage = "SPK-9";
                        return result;
                    }
                }

                int noAffectedRowsStep2 = 0;
                if (changePassphrase == true)
                {
                    string hashedPassphrase = HelperFunctions.hashPassphrase(userPassphrase);
                    updateComm.CommandText = @"UPDATE ""wgr_sp_kontrolleur"" SET
                                    pwd=@pwd WHERE e_mail=@e_mail";
                    updateComm.Parameters.AddWithValue("pwd", hashedPassphrase);
                    updateComm.Parameters.AddWithValue("e_mail", user.mailAddress);
                    noAffectedRowsStep2 = updateComm.ExecuteNonQuery();
                }

                pgConn.Close();

                if (noAffectedRowsStep1 == 1 &&
                    (!changePassphrase || noAffectedRowsStep2 == 1))
                {
                    return user;
                }

            }

        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            result.errorMessage = "SPK-3";
            return result;
        }

        _logger.LogError("Fatal error in update user process");
        result.errorMessage = "SPK-3";
        return result;
    }

    public ErrorMessage DeleteUser(string email)
    {
        ErrorMessage errorResult = new ErrorMessage();

        if (email == null)
        {
            _logger.LogWarning("No user data provided by user in delete user process. " +
                        "Thus process is canceled, no user is deleted.");
            errorResult.errorMessage = "SPK-3";
            return errorResult;
        }

        email = email.ToLower().Trim();

        if (email == "")
        {
            _logger.LogWarning("No user data provided by user in delete user process. " +
                        "Thus process is canceled, no user is deleted.");
            errorResult.errorMessage = "SPK-3";
            return errorResult;
        }

        User userInDb;
        ActionResult<User[]> usersInDbResult = this.GetUsers(email);
        User[]? usersInDb = usersInDbResult.Value;
        if (usersInDb == null || usersInDb.Length != 1 || usersInDb[0] == null)
        {
            _logger.LogWarning("User " + email + " cannot be deleted since this user is not in the database.");
            errorResult.errorMessage = "SPK-3";
            return errorResult;
        }
        else
        {
            userInDb = usersInDb[0];
            if (userInDb.role == "administrator")
            {
                if (_countNumberOfActiveAdmins() == 1)
                {
                    _logger.LogWarning("User tried to delete last administrator. Last administrator cannot be removed.");
                    errorResult.errorMessage = "SPK-3";
                    return errorResult;
                }
            }
            using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
            {
                pgConn.Open();
                NpgsqlCommand updateComm = pgConn.CreateCommand();
                updateComm.CommandText = @"UPDATE ""wgr_sp_kontrolleur""
                                SET aktiv=false
                                WHERE e_mail=@e_mail";
                updateComm.Parameters.AddWithValue("e_mail", email);

                int noAffectedRows = updateComm.ExecuteNonQuery();

                pgConn.Close();

                if (noAffectedRows == 1)
                {
                    return errorResult;
                }
            }
        }

        _logger.LogError("Fatal error.");
        errorResult.errorMessage = "SPK-3";
        return errorResult;
    }

    private static int _countNumberOfActiveAdmins()
    {
        int count = 0;
        using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
        {
            pgConn.Open();
            NpgsqlCommand selectComm = pgConn.CreateCommand();
            selectComm.CommandText = @"SELECT count(*) 
                            FROM ""wgr_sp_kontrolleur""
                            WHERE aktiv=true AND rolle='administrator'";

            using (NpgsqlDataReader reader = selectComm.ExecuteReader())
            {
                while (reader.Read())
                {
                    count = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
                }
            }
            pgConn.Close();
        }
        return count;
    }

}

