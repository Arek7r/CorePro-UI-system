namespace CorePro
{
    public struct ValidationResult
    {
        public bool IsValid { get; }
        public string Message { get; }
        public int ErrorCode { get; }

        private ValidationResult(bool valid, string message, int errorCode)
        {
            IsValid = valid;
            Message = message;
            ErrorCode = errorCode;
        }

        public static ValidationResult Valid() => new ValidationResult(true, null, 0);
        public static ValidationResult Invalid(string msg, int code = -1) => new ValidationResult(false, msg, code);
    }
}