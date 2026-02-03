namespace txuribeltz.Services;

public interface IAuthService
{
    Task<bool> ValidateUserAsync(string username, string password);
    Task<bool> IsAdminAsync(string username);
}