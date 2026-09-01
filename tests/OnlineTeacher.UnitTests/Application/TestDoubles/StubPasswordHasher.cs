using OnlineTeacher.Application.Security;

namespace OnlineTeacher.UnitTests.Application.TestDoubles;

internal sealed class StubPasswordHasher : IPasswordHasher
{
    public string Hash(string password) => $"hashed:{password}";

    public bool Verify(string password, string storedHash) => storedHash == $"hashed:{password}";
}