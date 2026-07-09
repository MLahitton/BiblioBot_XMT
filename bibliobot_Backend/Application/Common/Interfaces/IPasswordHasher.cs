using Domain.Entities;

namespace Application.Common.Interfaces;

public interface IPasswordHasher
{
    string HashPassword(User user, string password);
    bool VerifyPassword(User user, string password);
}
