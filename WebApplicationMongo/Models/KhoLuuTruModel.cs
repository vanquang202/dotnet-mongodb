using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebApplicationMongo.Models
{
    public class KhoLuuTruModel
    {
    }

    public class HoSoModel : KhoLuuTruModel
    {
        [BsonId]
        public ObjectId Id { get; set; }

        [BsonElement("filecode")]
        public string filecode { get; set; }
        [BsonElement("title")]
        public string title { get; set; }
        [BsonElement("description")]
        public string description { get; set; }
        [BsonElement("updated_at")]
        public DateTime updated_at { get; set; }
        [BsonElement("created_at")]
        public DateTime created_at
        {
            get; set;
        }
        [BsonElement("vanban")]
        public List<VanBanModel> vanban
        {
            get; set;
        }
    }

    public class VanBanModel : KhoLuuTruModel
    {
        [BsonId]
        public ObjectId Id { get; set; }

        [BsonElement("filecode")]
        public string filecode { get; set; }
        [BsonElement("title")]
        public string title { get; set; }
        [BsonElement("description")]
        public string description { get; set; }
        [BsonElement("hoso_id")]
        public ObjectId hoso_id { get; set; }
        [BsonElement("files")]
        public List<FileModel> files { get; set; }
        [BsonElement("updated_at")]
        public DateTime updated_at { get; set; }
        [BsonElement("created_at")]
        public DateTime created_at
        {
            get; set;
        }
    }

    public class FileModel
    {
        [BsonElement("filename")]
        public string filename { get; set; }
        [BsonElement("mime")]
        public string mime { get; set; }
        [BsonElement("size")]
        public Int32 size { get; set; }
        [BsonElement("data")]
        public string data { get; set; }
    }

    public class TestModel : BsonDocument
    {
        [BsonId]
        public ObjectId Id { get; set; }

        [BsonElement("name")]
        public string name { get; set; }
    }
}