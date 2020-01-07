using System.Collections.Generic;
using System.Linq;

namespace MediaEngine.Unpackers
{
    public enum ClassType
    {
        TextCast = 131
    }

    public static class ApiFunctions
    {
        public static Dictionary<short, ApiFunction> All { get; }

        static ApiFunctions()
        {
            All = new[]
            {
                new ApiFunction(ClassType.TextCast, 32, "Text", ApiFunctionType.Property),
                new ApiFunction(ClassType.TextCast, 40, "Method40", ApiFunctionType.MethodNoParams),
                new ApiFunction(ClassType.TextCast, 48, "Font.Color.R", ApiFunctionType.Property),
                new ApiFunction(ClassType.TextCast, 49, "Font.Color.G", ApiFunctionType.Property),
                new ApiFunction(ClassType.TextCast, 50, "Font.Color.B", ApiFunctionType.Property),
            }
            .ToDictionary(f => f.Key);
        }
    }
}