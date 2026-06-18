namespace BudgetPilot.Domain.Exceptions;

/// <summary>
/// Wird bei Verletzung einer fachlichen Invariante geworfen (z. B. überschneidende
/// Versionen, zwei gleichzeitig gültige Versionen, ungültige Validierungswerte).
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message)
    {
    }

    public DomainException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
