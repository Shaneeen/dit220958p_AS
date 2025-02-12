using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

public class ErrorModel : PageModel
{
    private readonly ILogger<ErrorModel> _logger;

    public string ErrorTitle { get; set; }
    public string ErrorMessage { get; set; }

    public ErrorModel(ILogger<ErrorModel> logger)
    {
        _logger = logger;
    }

    public void OnGet(int? code = null)
    {
        if (code.HasValue)
        {
            switch (code.Value)
            {
                case 404:
                    ErrorTitle = "404 - Page Not Found";
                    ErrorMessage = "Sorry, the page you are looking for doesn't exist.";
                    break;
                case 403:
                    ErrorTitle = "403 - Access Denied";
                    ErrorMessage = "You don't have permission to access this page.";
                    break;
                case 401:
                    ErrorTitle = "401 - Unauthorized";
                    ErrorMessage = "You need to log in to access this page.";
                    break;
                case 400:
                    ErrorTitle = "400 - Bad Request";
                    ErrorMessage = "The request could not be understood by the server.";
                    break;
                default:
                    ErrorTitle = $"{code} - Unexpected Error";
                    ErrorMessage = "An unexpected error has occurred.";
                    break;
            }
        }
        else
        {
            // For unhandled exceptions (500)
            ErrorTitle = "500 - Internal Server Error";
            ErrorMessage = "An unexpected server error occurred. Please try again later.";
        }

        _logger.LogError($"Error occurred with status code: {code ?? 500}");
    }
}
