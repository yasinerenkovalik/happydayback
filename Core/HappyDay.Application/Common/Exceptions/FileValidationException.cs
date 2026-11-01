namespace HappyDay.Application.Common.Exceptions;

/// <summary>Yüklenen dosya kabul edilmediğinde fırlatılır; API bunu 400 olarak döner.</summary>
public class FileValidationException : Exception
{
    public FileValidationException(string message) : base(message)
    {
    }
}
