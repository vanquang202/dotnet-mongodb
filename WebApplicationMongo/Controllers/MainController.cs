using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Xml.Linq;
using Amazon.Runtime.Documents;
using Microsoft.Ajax.Utilities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using WebApplicationMongo.Models;
using static System.Net.WebRequestMethods;

namespace WebApplicationMongo.Controllers
{
    public class MainController : ApiController
    {
        //private MongoClient client = new MongoClient("mongodb+srv://root:1111@atlascluster.felyftz.mongodb.net/?authSource=admin");
        private MongoClient client = new MongoClient("mongodb://localhost:27017");
        private IMongoCollection<BsonDocument> collectionConnect;
        protected dynamic Collection(string collectionStr)
        {
            IMongoDatabase database = this.client.GetDatabase("kholuutru");
            this.collectionConnect = database.GetCollection<BsonDocument>(collectionStr);
            return this;
        }

        protected IMongoDatabase connectDatabseMongo()
        {
            IMongoDatabase database = this.client.GetDatabase("kholuutru");
            return database;
        }

        private BsonDocument filterData = new BsonDocument();
        private BsonDocument sortData = new BsonDocument();
        private BsonDocument document = new BsonDocument();
        private BsonDocument metaData = new BsonDocument();
        protected dynamic AddFilter(string name, BsonValue value)
        {
            this.filterData.Add(name, value);
            return this;
        }
        protected dynamic AddOrFilter(BsonArray filters)
        {
            this.filterData.Add("$or", filters);
            return this;
        }
        protected dynamic AddSort(string name, BsonValue value)
        {
            this.sortData.Add(name, value);
            return this;
        }
        protected dynamic Add(string name, BsonValue value)
        {
            this.document.Add(name, value);
            return this;
        }
        private List<BsonDocument> s_pipeLine = new List<BsonDocument>();
        protected dynamic AddRelation(string collection, string localField, string foreignField, string asStr, bool isUnWind = false)
        {
            var document = new BsonDocument
            {
                    { "from", collection },
                    { "localField", localField },
                    { "foreignField",foreignField },
                    { "as",asStr }
            };
            var s_lookUp = new BsonDocument
            {
            };

            s_lookUp.Add("$lookup", document);
            this.s_pipeLine.Add(s_lookUp);

            if (isUnWind)
            {
                var s_unWind = new BsonDocument
                {
                };
                s_unWind.Add("$unwind", "$" + asStr);
                this.s_pipeLine.Add(s_unWind);
            }
            return this;
        }

        protected dynamic AddMatch()
        {
            var s_matchWhere = new BsonDocument
            { };
            s_matchWhere.Add("$match", this.filterData);
            this.s_pipeLine.Add(s_matchWhere);
            return this;
        }
        protected dynamic ConvertDocument(BsonDocument cv)
        {
            var s_convertDoc = new BsonDocument
            { };
            s_convertDoc.Add("$project", cv);
            this.s_pipeLine.Add(s_convertDoc);
            return this;
        }


        protected async Task<IEnumerable<T>> S_Aggregate<T>()
        {
            List<BsonDocument> data = await collectionConnect.Aggregate<BsonDocument>(this.s_pipeLine).ToListAsync();
            IEnumerable<T> dataS = data.Select(x => BsonSerializer.Deserialize<T>(x)).ToList();
            return dataS;
        }
        protected async Task<IEnumerable<T>> GetAll<T>(int limit = 0, int skip = 0)
        {
            List<BsonDocument> data = await collectionConnect.Find(this.filterData).Sort(this.sortData).Limit(limit).Skip(skip).ToListAsync();
            IEnumerable<T> dataS = data.Select(x => BsonSerializer.Deserialize<T>(x)).ToList();
            return dataS;
        }
        protected async Task<T> Get<T>(BsonDocument filter)
        {
            BsonDocument data = await collectionConnect.Find(filter).FirstOrDefaultAsync();
            return BsonSerializer.Deserialize<T>(data);
        }

        protected async Task<Boolean> Store()
        {
            await collectionConnect.InsertOneAsync(this.document);
            return true;
        }
        protected async Task<Boolean> Update(BsonDocument filter)
        {
            var dataUpdate = new BsonDocument();
            dataUpdate.Add("$set", this.document);
            await collectionConnect.UpdateOneAsync(filter, dataUpdate);
            return true;
        }
        protected async Task<Boolean> Delete(BsonDocument filter)
        {
            await collectionConnect.DeleteOneAsync(filter);
            return true;
        }
        protected async Task<long> Total()
        {
            var total = await collectionConnect.Find(new BsonDocument()).CountAsync();
            return total;
        }

        protected dynamic StartForLoop()
        {
            this.metaData = new BsonDocument();
            return this;
        }

        protected dynamic AddOptionsMetadata(string name, BsonValue value)
        {
            this.metaData.Add(name, value);
            return this;
        }

        protected async Task<ObjectId> UploadFileGFSBucket(dynamic file)
        {
            var database = this.connectDatabseMongo();
            var bucket = new GridFSBucket(database);
            var options = new GridFSUploadOptions
            {
                Metadata = this.metaData
            };
            var fileId = await bucket.UploadFromStreamAsync(file.FileName, file.InputStream, options);
            return fileId;
        }

        protected async Task<dynamic> GetBaseStringByFileID(ObjectId id)
        {
            var database = this.connectDatabseMongo();
            var bucket = new GridFSBucket(database);
            var filter = Builders<GridFSFileInfo<ObjectId>>.Filter.Eq(x => x.Id, id);
            var fileInfo = await bucket.Find(filter).FirstOrDefaultAsync();
            if (fileInfo == null) return null;

            using (var stream = await bucket.OpenDownloadStreamAsync(id))
            {
                using (var memoryStream = new MemoryStream())
                {
                    //using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Compress))
                    //{
                    //    await stream.CopyToAsync(gzipStream);
                    //}
                    const int bufferSize = 1024 * 1024;
                    var buffer = new byte[bufferSize];
                    var base64Chunks = new List<string>();
                    while (true)
                    {
                        var bytesRead = await stream.ReadAsync(buffer, 0, bufferSize);

                        if (bytesRead == 0)
                        {
                            break;
                        }

                        var base64Chunk = Convert.ToBase64String(buffer, 0, bytesRead);
                        base64Chunks.Add(base64Chunk);
                    }

                    //await stream.CopyToAsync(memoryStream);
                    //var base64String = Convert.ToBase64String(memoryStream.ToArray());
                    return base64Chunks;
                }
            }
        }
    }
}