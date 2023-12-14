namespace Dec
{
    public class ConverterReadException : System.Exception
    {
        public ConverterReadException(InputContext inputContext, object converter, System.Exception innerException)
            : base($"{inputContext}: Exception thrown by {converter}", innerException) { }
    }
}
