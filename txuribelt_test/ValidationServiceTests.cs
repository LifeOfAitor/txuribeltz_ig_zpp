using txuribeltz.Services;
using Xunit;

namespace txuribelt_test;

public class ValidationServiceTests
{
    private readonly ValidationService _svc = new();

    #region Login Validation Tests (MainWindow)

    [Fact]
    public void ValidateLogin_EmptyUsername_ReturnsFail()
    {
        var result = _svc.ValidateLogin("", "password");

        Assert.False(result.IsValid);
        Assert.Equal("Erabiltzaile edo pasahitza hutsik daude.", result.ErrorMessage);
    }

    [Fact]
    public void ValidateLogin_EmptyPassword_ReturnsFail()
    {
        var result = _svc.ValidateLogin("user", "");

        Assert.False(result.IsValid);
        Assert.Equal("Erabiltzaile edo pasahitza hutsik daude.", result.ErrorMessage);
    }

    [Fact]
    public void ValidateLogin_BothEmpty_ReturnsFail()
    {
        var result = _svc.ValidateLogin("", "");

        Assert.False(result.IsValid);
        Assert.Contains("hutsik", result.ErrorMessage);
    }

    [Fact]
    public void ValidateLogin_WhitespaceOnly_ReturnsFail()
    {
        var result = _svc.ValidateLogin("   ", "   ");

        Assert.False(result.IsValid);
    }

    [Fact]
    public void ValidateLogin_NullUsername_ReturnsFail()
    {
        var result = _svc.ValidateLogin(null!, "password");

        Assert.False(result.IsValid);
    }

    [Fact]
    public void ValidateLogin_ValidCredentials_ReturnsSuccess()
    {
        var result = _svc.ValidateLogin("user", "password");

        Assert.True(result.IsValid);
        Assert.Null(result.ErrorMessage);
    }

    #endregion

    #region Signup Validation Tests (SingUp Window)

    [Fact]
    public void ValidateSignup_EmptyUsername_ReturnsFail()
    {
        var result = _svc.ValidateSignup("", "pass", "pass");

        Assert.False(result.IsValid);
        Assert.Equal("Erabiltzaile edo pasahitza hutsik daude.", result.ErrorMessage);
    }

    [Fact]
    public void ValidateSignup_EmptyPassword_ReturnsFail()
    {
        var result = _svc.ValidateSignup("user", "", "");

        Assert.False(result.IsValid);
        Assert.Contains("hutsik", result.ErrorMessage);
    }

    [Fact]
    public void ValidateSignup_PasswordMismatch_ReturnsFail()
    {
        var result = _svc.ValidateSignup("user", "password1", "password2");

        Assert.False(result.IsValid);
        Assert.Equal("Pasahitzak ez datoz bat.", result.ErrorMessage);
    }

    [Fact]
    public void ValidateSignup_PasswordMismatch_CaseSensitive_ReturnsFail()
    {
        var result = _svc.ValidateSignup("user", "Password", "password");

        Assert.False(result.IsValid);
        Assert.Contains("ez datoz bat", result.ErrorMessage);
    }

    [Fact]
    public void ValidateSignup_EmptyConfirmPassword_ReturnsFail()
    {
        var result = _svc.ValidateSignup("user", "pass", "");

        Assert.False(result.IsValid);
    }

    [Fact]
    public void ValidateSignup_ValidData_ReturnsSuccess()
    {
        var result = _svc.ValidateSignup("newuser", "password123", "password123");

        Assert.True(result.IsValid);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void ValidateSignup_WhitespaceUsername_ReturnsFail()
    {
        var result = _svc.ValidateSignup("   ", "pass", "pass");

        Assert.False(result.IsValid);
    }

    #endregion
}