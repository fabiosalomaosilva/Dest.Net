namespace Dest.Models
{
    public class ClassCode
    {
        public string ClassName { get; set; }
        public string RelativePath { get; set; }
        public string ClassText { get; set; }
        public HashSet<string> RelatedClasses { get; set; } = new HashSet<string>();

    }
}
