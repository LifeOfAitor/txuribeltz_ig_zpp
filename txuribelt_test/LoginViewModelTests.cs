using Moq;
using txuribeltz.Services;
using txuribeltz.ViewModels;
using Xunit;

namespace txuribelt_test;

public class LoginViewModelTests
{
    private readonly Mock<IAuthService> _authServiceMock;
    private readonly ValidationService _validationService;

    public LoginViewModelTests()
    {
        _authServiceMock = new Mock<IAuthService>();
        _validationService = new ValidationService();
    }

    [Fact]
    public async Task LoginAsync_EmptyUsername_ReturnsFalseWithError()
    {
        var vm = new LoginViewModel(_authServiceMock.Object, _validationService);
        vm.Username = "";
        vm.Password = "password";

        var result = await vm.LoginAsync();

        Assert.False(result);
        Assert.False(vm.IsAuthenticated);
        Assert.Contains("hutsik", vm.ErrorMessage);
    }

    [Fact]
    public async Task LoginAsync_EmptyPassword_ReturnsFalseWithError()
    {
        var vm = new LoginViewModel(_authServiceMock.Object, _validationService);
        vm.Username = "user";
        vm.Password = "";

        var result = await vm.LoginAsync();

        Assert.False(result);
        Assert.False(vm.IsAuthenticated);
        Assert.Contains("hutsik", vm.ErrorMessage);
    }

    [Fact]
    public async Task LoginAsync_InvalidCredentials_ReturnsFalseWithError()
    {
        _authServiceMock.Setup(s => s.ValidateUserAsync("alice", "wrongpass"))
            .ReturnsAsync(false);

        var vm = new LoginViewModel(_authServiceMock.Object, _validationService);
        vm.Username = "alice";
        vm.Password = "wrongpass";

        var result = await vm.LoginAsync();

        Assert.False(result);
        Assert.False(vm.IsAuthenticated);
        Assert.Equal("Erabiltzailea edo pasahitza okerra", vm.ErrorMessage);
        _authServiceMock.Verify(s => s.IsAdminAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_ValidUser_ReturnsTrueAndIsNotAdmin()
    {
        _authServiceMock.Setup(s => s.ValidateUserAsync("bob", "pass"))
            .ReturnsAsync(true);
        _authServiceMock.Setup(s => s.IsAdminAsync("bob"))
            .ReturnsAsync(false);

        var vm = new LoginViewModel(_authServiceMock.Object, _validationService);
        vm.Username = "bob";
        vm.Password = "pass";

        var result = await vm.LoginAsync();

        Assert.True(result);
        Assert.True(vm.IsAuthenticated);
        Assert.False(vm.IsAdmin);
        Assert.Empty(vm.ErrorMessage);
    }

    [Fact]
    public async Task LoginAsync_ValidAdmin_ReturnsTrueAndIsAdmin()
    {
        _authServiceMock.Setup(s => s.ValidateUserAsync("admin", "admin"))
            .ReturnsAsync(true);
        _authServiceMock.Setup(s => s.IsAdminAsync("admin"))
            .ReturnsAsync(true);

        var vm = new LoginViewModel(_authServiceMock.Object, _validationService);
        vm.Username = "admin";
        vm.Password = "admin";

        var result = await vm.LoginAsync();

        Assert.True(result);
        Assert.True(vm.IsAuthenticated);
        Assert.True(vm.IsAdmin);
        Assert.Empty(vm.ErrorMessage);
    }

    [Fact]
    public async Task LoginAsync_ConnectionException_ReturnsFalseWithError()
    {
        _authServiceMock.Setup(s => s.ValidateUserAsync("user", "pass"))
            .ThrowsAsync(new InvalidOperationException("Konexio errorea"));

        var vm = new LoginViewModel(_authServiceMock.Object, _validationService);
        vm.Username = "user";
        vm.Password = "pass";

        var result = await vm.LoginAsync();

        Assert.False(result);
        Assert.False(vm.IsAuthenticated);
        Assert.Equal("Konexio errorea", vm.ErrorMessage);
    }
}