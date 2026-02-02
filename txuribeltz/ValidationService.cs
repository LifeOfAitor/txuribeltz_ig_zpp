namespace txuribeltz;

public record ValidationResult(bool IsValid, string? ErrorMessage = null);

public class ValidationService
{
    /// <summary>
    /// Validates login form inputs
    /// </summary>
    public ValidationResult ValidateLogin(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return new ValidationResult(false, "Erabiltzaile edo pasahitza hutsik daude.");

        return new ValidationResult(true);
    }

    /// <summary>
    /// Validates signup form inputs
    /// </summary>
    public ValidationResult ValidateSignup(string username, string password, string confirmPassword)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return new ValidationResult(false, "Erabiltzaile edo pasahitza hutsik daude.");

        if (password != confirmPassword)
            return new ValidationResult(false, "Pasahitzak ez datoz bat.");

        return new ValidationResult(true);
    }
}