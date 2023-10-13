using System.Diagnostics.CodeAnalysis;

namespace Dest.Utils
{
    public static class Extensions
    {

        public static bool HasAttribute(this Type type, Type attributeType)
        {
            return type.GetCustomAttributes(attributeType, true).Length > 0;
        }

        public static bool IsExcludeFromCodeCoverage(this Type type)
        {
            return type.HasAttribute(typeof(ExcludeFromCodeCoverageAttribute));
        }
    }
}
