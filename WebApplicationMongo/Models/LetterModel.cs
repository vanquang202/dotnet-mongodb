using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebApplicationMongo.Models
{
    public class Result<T>
    {
        public List<T> data { get; set; }
    }
    public class File
    {

        [BsonElement("filename")]
        public string filename { get; set; }
        [BsonElement("mime")]
        public string mime { get; set; }
        [BsonElement("size")]
        public Int32 size { get; set; }

        [BsonElement("data")]
        public byte[] data { get; set; }

    }
    public class LetterModel
    {
        [BsonId]
        public string Id { get; set; }

        [BsonElement("email")]
        public string email { get; set; }
        [BsonElement("user_id")]
        public string user_id { get; set; }
        [BsonElement("content")]
        public string content { get; set; }
        [BsonElement("subject")]
        public string subject { get; set; }
        [BsonElement("files")]
        public List<File> files { get; set; }
        [BsonElement("updated_at")]
        public DateTime updated_at { get; set; }
        [BsonElement("created_at")]
        public DateTime created_at { get; set; }

    }
}