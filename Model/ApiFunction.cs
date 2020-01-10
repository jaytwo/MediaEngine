namespace MediaEngine.Unpackers
{
    public enum ApiFunctionType
    {
        Property,
        PropertyInt,
        MethodNoParams
    }

    public class ApiFunction
    {
        public ClassType ClassType { get; }
        public byte PropertyNumber { get; }
        public string PropertyName { get; }
        public ApiFunctionType FunctionType { get; }

        public short Key => (short)((short)ClassType * 256 + PropertyNumber);

        public ApiFunction(ClassType classType, byte propertyNumber, string propertyName, ApiFunctionType functionType)
        {
            ClassType = classType;
            PropertyNumber = propertyNumber;
            PropertyName = propertyName;
            FunctionType = functionType;
        }
    }
}
