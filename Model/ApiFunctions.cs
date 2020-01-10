using System.Collections.Generic;
using System.Linq;

namespace MediaEngine.Unpackers
{
    public enum ClassType
    {
        Class97 = 97,
        TextCast = 131
    }

    public static class ApiFunctions
    {
        public static Dictionary<short, ApiFunction> All { get; }

        static ApiFunctions()
        {
            All = new[]
            {
                new ApiFunction(ClassType.Class97, 64, "64", ApiFunctionType.PropertyInt),
                new ApiFunction(ClassType.TextCast, 32, "Text", ApiFunctionType.Property),
                new ApiFunction(ClassType.TextCast, 40, "40", ApiFunctionType.MethodNoParams),
                new ApiFunction(ClassType.TextCast, 48, "Font.Color.R", ApiFunctionType.Property),
                new ApiFunction(ClassType.TextCast, 49, "Font.Color.G", ApiFunctionType.Property),
                new ApiFunction(ClassType.TextCast, 50, "Font.Color.B", ApiFunctionType.Property),
            }
            .ToDictionary(f => f.Key);
        }
    }
}