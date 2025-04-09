// <copyright company="Geoinformation Winterthur">
//      Author: Edgar Butwilowski
//      Copyright (c) Geoinformation Winterthur. All rights reserved.
// </copyright>
using playground_check.Model;

namespace playground_check.Service;

public interface IUserService
{
    public User GetUser(string email);
    public User[] GetUsers(string? email);
    public User UpdateUser(User user, bool changePassphrase = false);
    public ErrorMessage DeleteUser(string email);
}

