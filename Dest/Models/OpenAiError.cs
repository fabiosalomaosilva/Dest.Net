namespace Dest.Models;

public class OpenAiError
{
    public ErrorDetails Error { get; set; }

    public class ErrorDetails
    {
        public string Type { get; set; }
        public string Message { get; set; }
    }
}