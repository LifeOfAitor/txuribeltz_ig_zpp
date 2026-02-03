using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using txuribeltz.Services;

namespace txuribeltz.ViewModels;

public class LoginViewModel : INotifyPropertyChanged
{
    private readonly IAuthService _authService;
    private readonly ValidationService _validationService;
    private string _username = string.Empty;
    private string _password = string.Empty;
    private string _errorMessage = string.Empty;
    private bool _isAuthenticated;
    private bool _isAdmin;

    public LoginViewModel(IAuthService authService, ValidationService validationService)
    {
        _authService = authService;
        _validationService = validationService;
    }

    public string Username
    {
        get => _username;
        set { _username = value; OnPropertyChanged(); }
    }

    public string Password
    {
        get => _password;
        set { _password = value; OnPropertyChanged(); }
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        private set { _errorMessage = value; OnPropertyChanged(); }
    }

    public bool IsAuthenticated
    {
        get => _isAuthenticated;
        private set { _isAuthenticated = value; OnPropertyChanged(); }
    }

    public bool IsAdmin
    {
        get => _isAdmin;
        private set { _isAdmin = value; OnPropertyChanged(); }
    }

    public async Task<bool> LoginAsync()
    {
        // Formularioaren balioztapena
        var validation = _validationService.ValidateLogin(Username, Password);
        if (!validation.IsValid)
        {
            ErrorMessage = validation.ErrorMessage!;
            IsAuthenticated = false;
            return false;
        }

        try
        {
            // Autentifikazioa zerbitzarian
            bool isValid = await _authService.ValidateUserAsync(Username, Password);
            if (!isValid)
            {
                ErrorMessage = "Erabiltzailea edo pasahitza okerra";
                IsAuthenticated = false;
                return false;
            }

            // Administratzailea den konprobatu
            IsAdmin = await _authService.IsAdminAsync(Username);
            IsAuthenticated = true;
            ErrorMessage = string.Empty;
            return true;
        }
        catch (InvalidOperationException ex)
        {
            ErrorMessage = ex.Message;
            IsAuthenticated = false;
            return false;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}