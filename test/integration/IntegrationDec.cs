namespace DecTestIntegration
{
    [Dec.StaticReferences]
    public static class IntegrationDecs
    {
        static IntegrationDecs()
        {
            Dec.StaticReferencesAttribute.Initialized();
        }

        public static IntegrationDec ItemAlpha;
    }

    public class IntegrationDec : Dec.Dec
    {

    }
}
