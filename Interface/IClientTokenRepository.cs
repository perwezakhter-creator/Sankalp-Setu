namespace SankalpAPI.Interface
{
    public interface IClientTokenRepository
    {
        Task SaveTokenAsync(string userId, string tokenHash);

        Task<string?> ValidateTokenAsync(string tokenHash);

        Task<bool> RevokeTokenAsync(string tokenHash);
    }
}
