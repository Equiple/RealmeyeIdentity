namespace RealmeyeIdentity.Authentication
{
    public class PasswordOptions
    {
        public int SaltLengthBytes { get; set; }

        public string Pepper { get; set; }

        public int HashLengthBytes { get; set; }

        public int HashMemoryKbytes { get; set; }

        public int HashIterations { get; set; }

        public int HashParallelismDegree { get; set; }
    }
}
