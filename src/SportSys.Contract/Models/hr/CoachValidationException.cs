namespace SportSys.Contract.Models.hr;

public class CoachValidationException : Exception
{
    public CoachValidationException(string message)
        : base(message)
    {
    }

    public CoachValidationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public CoachValidationException(IEnumerable<string> errors)
        : base(string.Join(Environment.NewLine, errors))
    {
        Errors = errors.ToArray();
    }

    public IReadOnlyList<string> Errors { get; } = [];
}
