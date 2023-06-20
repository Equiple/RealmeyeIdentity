using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Bson.Serialization.Serializers;

namespace RealmeyeIdentity.Authentication
{
    public static class UserBsonMap
    {
        public static void Register()
        {
            BsonClassMap.RegisterClassMap<User>(map =>
            {
                map.AutoMap();
                map.MapIdMember(user => user.Id)
                    .SetIdGenerator(StringObjectIdGenerator.Instance)
                    .SetSerializer(new StringSerializer(BsonType.ObjectId));
            });
        }
    }
}
