namespace Dest.Models
{
    public class OpenAiResponse
    {
        public List<Choice> Choices { get; set; }

        public class Choice
        {
            public string Text { get; set; }
        }
    }
}
