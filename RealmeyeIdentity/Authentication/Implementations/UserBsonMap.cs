using MongoDB.Bson.Serialization;

namespace RealmeyeIdentity.Authentication
{
    public static class UserBsonMap
    {
        public static void Register()
        {
            BsonClassMap.RegisterClassMap<User>(map =>
            {
                
            });
        }
    }
}
