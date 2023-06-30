namespace RealmeyeIdentity.Authentication
{
    public record class RegistrationSession(string Id, string Code, DateTimeOffset ExpiresAt)
    {
        public byte[] Serialize()
        {
            using MemoryStream stream = new();
            using BinaryWriter writer = new(stream);
            writer.Write(Id);
            writer.Write(Code);
            writer.Write(ExpiresAt.ToUnixTimeSeconds());
            return stream.ToArray();
        }

        public static RegistrationSession Deserialize(byte[] bytes)
        {
            using MemoryStream stream = new(bytes);
            using BinaryReader reader = new(stream);
            string id = reader.ReadString();
            string code = reader.ReadString();
            long expiresAt = reader.ReadInt64();
            return new RegistrationSession(id, code, DateTimeOffset.FromUnixTimeSeconds(expiresAt));
        }
    }
}
