namespace Data.Contracts
{
    public interface IPropertyIdentityService
    {

        public long GenerateNewValueIdentity(string propertyIdentity, string typeName);
        public List<long> GenerateNewValueIdentityRange(string propertyIdentity, string typeName , long skip);
    }
}
