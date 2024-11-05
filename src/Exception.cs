namespace Dec
{
    public class ConverterReadException : System.Exception
    {
        public ConverterReadException(Context context, object converter, System.Exception innerException)
            : base($"{context}: Exception thrown by {converter}", innerException) { }
    }
}
