namespace TBD.Shared.Utils;

public interface IHasher
{
    string HashPassword(string password);
    bool Verify(string encodedHash, string password);
}
