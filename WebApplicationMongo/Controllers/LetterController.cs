using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using System.Web.Http;
using Newtonsoft.Json.Linq;
using System.IO;
using Amazon.Runtime.Documents;
using Newtonsoft.Json;
using System.Web;
using Microsoft.SqlServer.Server;
using Amazon.Runtime.Internal;
using System.Linq;
using System.Xml.Linq;
using MongoDB.Driver.GridFS;

namespace WebApplicationMongo.Controllers
{
    [RoutePrefix("letter")]
    public class LetterController : MainController
    {
        [HttpGet]
        [Route("getall")]
        public async Task<IHttpActionResult> GetAll()
        {
            var db = this.Collection("letters");
            var request = HttpContext.Current.Request;
            db.AddSort("created_at", -1);
            var limit = 10;
            if (request.QueryString["subject"] != null)
            {
                string subject = request.QueryString["subject"];
                db.AddFilter("subject", new BsonRegularExpression(@"/" + subject + "/i"));
            }
            if (request.QueryString["step"] != null)
            {
                int step = int.Parse(request.QueryString["step"]);
                limit = limit * step;
            }
            var data = await db.GetAll(limit);
            var total = await db.Total();

            List<BsonDocument> dataResultList = new List<BsonDocument>();
            BsonDocument[] dataResult = dataResultList.ToArray();
            foreach (var document in data)
            {
                if (document.Contains("files"))
                {
                    var files = document.GetValue("files").AsBsonArray;
                    var newFiles = new BsonArray();
                    foreach (var file in files)
                    {


                        if (file["size"].AsInt32 < 16777216)
                        {
                            var binaryData = file["data"].AsBsonBinaryData;
                            var base64String = Convert.ToBase64String(binaryData.Bytes);
                            var newFile = new BsonDocument("filename", file["filename"].AsString)
                            .Add("mime", file["mime"].AsString)
                            .Add("data", base64String)
                            .Add("url", "data:" + file["mime"] + ";base64," + base64String);
                            newFiles.Add(newFile);
                        }
                        else
                        {
                            var base64String = await this.GetBaseStringByFileID(new ObjectId(file["data"].ToString()));
                            var newFile = new BsonDocument("filename", file["filename"].AsString)
                            .Add("mime", file["mime"].AsString)
                            .Add("data", base64String)
                            .Add("url", "data:" + file["mime"] + ";base64," + base64String);
                            newFiles.Add(newFile);
                        }


                    }
                    document["_id"] = document.GetValue("_id").ToString();
                    document["created_at"] = document.GetValue("created_at").ToString();
                    document["updated_at"] = document.GetValue("updated_at").ToString();
                    document["files"] = newFiles;
                    Array.Resize(ref dataResult, dataResult.Length + 1);
                    dataResult[dataResult.GetUpperBound(0)] = document;
                }
            }
            var json = dataResult.ToJson();
            return Ok(new { status = 2, payload = json, total = total });
        }

        [HttpPost]
        [Route("show")]
        public async Task<IHttpActionResult> Show()
        {
            var request = HttpContext.Current.Request;
            var connect = this.Collection("letters");
            var filter = new BsonDocument
            {
                { "_id", ObjectId.Parse(request.Form["_id"])}
            };
            var document = await connect.Find(filter).FirstOrDefaultAsync();
            if (document.Contains("files"))
            {
                var files = document.GetValue("files").AsBsonArray;
                var newFiles = new BsonArray();

                foreach (var file in files)
                {
                    if (file["size"].AsInt32 < 16777216)
                    {
                        var binaryData = file["data"].AsBsonBinaryData;
                        var base64String = Convert.ToBase64String(binaryData.Bytes);
                        var newFile = new BsonDocument("filename", file["filename"].AsString)
                        .Add("mime", file["mime"].AsString)
                        .Add("size", file["size"].AsInt32)
                        .Add("data", base64String)
                        .Add("url", "data:" + file["mime"] + ";base64," + base64String);
                        newFiles.Add(newFile);
                    }
                    else
                    {
                        var base64String = await this.GetBaseStringByFileID(new ObjectId(file["data"].ToString()));
                        var newFile = new BsonDocument("filename", file["filename"].AsString)
                        .Add("mime", file["mime"].AsString)
                        .Add("size", file["size"].AsInt32)
                        .Add("data", base64String)
                        .Add("url", "data:" + file["mime"] + ";base64," + base64String);
                        newFiles.Add(newFile);
                    }
                }
                document["_id"] = document.GetValue("_id").ToString();
                document["created_at"] = document.GetValue("created_at").ToString();
                document["updated_at"] = document.GetValue("updated_at").ToString();
                document["files"] = newFiles;
            }

            return Ok(new { status = 1, payload = document.ToJson() });
        }

        private async Task<ObjectId> UploadFileGridFSBucket(dynamic file)
        {
            var database = this.connectDatabseMongo();
            var bucket = new GridFSBucket(database);
            var options = new GridFSUploadOptions
            {
                Metadata = new BsonDocument {
                  { "contentType", file.ContentType },
                  { "other", "value" },
                  { "another", "value2" }
               }
            };
            var fileId = await bucket.UploadFromStreamAsync(file.FileName, file.InputStream, options);
            return fileId;
        }

        private async Task<string> GetBaseStringByFileID(ObjectId id)
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
                    await stream.CopyToAsync(memoryStream);

                    var base64String = Convert.ToBase64String(memoryStream.ToArray());
                    return base64String;
                }
            }
        }

        [HttpPost]
        [Route("store")]
        public async Task<IHttpActionResult> Store()
        {
            var request = HttpContext.Current.Request;
            var files = request.Files.GetMultiple("datafiles[]");
            BsonArray dataFilesArray = new BsonArray();

            foreach (var file in files)
            {


                var fileName = file.FileName;
                var mime = file.ContentType;
                var size = file.ContentLength;
                if (size < 16777216)
                {
                    byte[] fileData = new byte[file.ContentLength];
                    file.InputStream.Read(fileData, 0, fileData.Length);
                    var binData = new BsonBinaryData(fileData);
                    var docFile = new BsonDocument
                    {
                         {"filename", fileName },
                         {"mime" ,  mime},
                         {"size" ,  size},
                         {"data", binData }
                    };
                    dataFilesArray.Add(docFile);
                }
                else
                {
                    var dataFileId = await this.UploadFileGridFSBucket(file);
                    var docFile = new BsonDocument
                    {
                         {"filename", fileName },
                         {"mime" ,  mime},
                         {"size" ,  size},
                         {"data", dataFileId.ToString()}
                    };
                    dataFilesArray.Add(docFile);
                }

            }
            var document = new BsonDocument
            {
                { "user_id" , request.Form["user_id"]  },
                { "email" , request.Form["email"]  },
                { "subject" , request.Form["subject"]  },
                { "content" , request.Form["content"]  },
                { "files" , dataFilesArray },
                { "created_at" , DateTime.Now },
                { "updated_at" , DateTime.Now },
            };
            var collection = this.Collection("letters");
            await collection.InsertOneAsync(document);
            return Ok(new { status = 1 });
        }

        [HttpPost]
        [Route("update")]
        public async Task<IHttpActionResult> Update()
        {
            var request = HttpContext.Current.Request;
            var db = this.Collection("letters");
            var database = this.connectDatabseMongo();
            var bucket = new GridFSBucket(database);
            var filter = new BsonDocument
            {
                { "_id", ObjectId.Parse(request.Form["_id"])}
            };
            var document = await db.Get(filter);
            BsonArray dataFilesArray = new BsonArray();
            var filesTemp = document.GetValue("files").AsBsonArray;

            var newFiles = new BsonArray();
            foreach (var file in filesTemp)
            {
                var flag = false;
                string fileOldsStr = request.Form["dataFileOld"];
                string[] fileOlds = fileOldsStr.Split('@');
                foreach (var fileOle in fileOlds)
                {
                    if (fileOle == file["filename"].ToString())
                    {
                        flag = true;
                        dataFilesArray.Add(file);
                    }
                }
                if (!flag && file["size"].ToInt32() > 16777216)
                {
                    await bucket.DeleteAsync(new ObjectId(file["data"].ToString()));
                }
            }

            var files = request.Files.GetMultiple("datafiles[]");

            foreach (var file in files)
            {
                var fileName = file.FileName;
                var mime = file.ContentType;
                var size = file.ContentLength;
                if (size < 16777216)
                {
                    byte[] fileData = new byte[file.ContentLength];
                    file.InputStream.Read(fileData, 0, fileData.Length);
                    var binData = new BsonBinaryData(fileData);
                    var docFile = new BsonDocument
                    {
                         {"filename", fileName },
                         {"mime" ,  mime},
                         {"size" ,  size},
                         {"data", binData }
                    };
                    dataFilesArray.Add(docFile);
                }
                else
                {
                    var dataFileId = await this.UploadFileGridFSBucket(file);
                    var docFile = new BsonDocument
                    {
                         {"filename", fileName },
                         {"mime" ,  mime},
                         {"size" ,  size},
                         {"data", dataFileId.ToString()}
                    };
                    dataFilesArray.Add(docFile);
                }
            }
            //var documentUpdate = Builders<BsonDocument>.Update
            //    .Set("user_id", request.Form["user_id"])
            //    .Set("email", request.Form["email"])
            //    .Set("content", request.Form["content"])
            //    .Set("updated_at", DateTime.Now)
            //    .Set("files", dataFilesArray)
            //    .Set("subject", request.Form["subject"]);
            db.Add("user_id", request.Form["user_id"])
                .Add("email", request.Form["email"])
                .Add("content", request.Form["content"])
                .Add("updated_at", DateTime.Now)
                .Add("files", dataFilesArray)
                .Add("subject", request.Form["subject"]);
            await db.Update(filter);
            return Ok(new { status = 1 });
        }



        [HttpPost]
        [Route("delete")]
        public async Task<IHttpActionResult> Delete()
        {
            var request = HttpContext.Current.Request;
            var connect = this.Collection("letters");
            var filter = new BsonDocument
            {
                { "_id", ObjectId.Parse(request.Form["_id"])}
            };
            connect.DeleteOne(filter);
            return Ok(new { status = 1 });
        }
    }
}