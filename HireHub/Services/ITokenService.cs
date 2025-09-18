public interface ITokenService
{
    string CreateToken(Guid userId, string role, string email);
}
