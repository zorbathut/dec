namespace Dec
{
    // Intentionally placed in the "Dec" namespace to exercise the embedded-source case where a user shoves their own code into Dec's namespace. Identity-based filtering in UtilReflection.GetAllUserTypes should surface this; namespace-based filtering would drop it.
    public class DecNamespaceUserType { }
}
