using Domain.Socioboard.Helpers;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Domain.Socioboard.Models.Mongo
{
    [BsonIgnoreExtraElements]
    public class TwitterRecentDetails
    {
        [BsonId]
        [JsonConverter(typeof(ObjectIdConverter))]
        public ObjectId Id { get; set; }
        public string strId { get; set; }
        public long retweetcount { get; set; }
        public long favoritecount { get; set; }
        public string FeedId { get; set; }
        public string lastfeed { get; set; }
        public string LastActivityDate { get; set; }
        public string AccountCreationDate { get; set; }
        public string TwitterId { get; set; }
    }
}
