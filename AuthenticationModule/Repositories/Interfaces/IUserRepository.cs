using AuthenticationModule.Repositories.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace AuthenticationModule.Repositories.Interfaces
{
    public interface IUserRepository
    {
       Task AddUser(User user);
       Task UpdateUser(User user);
       Task DeleteUser(User user);
       Task<User?> GetUserByEmail(string email);
    }
}
