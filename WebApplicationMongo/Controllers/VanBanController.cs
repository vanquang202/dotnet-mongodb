using Microsoft.Ajax.Utilities;
using MongoDB.Bson;
using MongoDB.Driver.GridFS;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using WebApplicationMongo.Models;

namespace WebApplicationMongo.Controllers
{
    [RoutePrefix("vanban")]
    public class VanBanController : MainController
    {
        [HttpPost]
        [Route("show")]
        public async Task<IHttpActionResult> Show()
        {
            var request = HttpContext.Current.Request;
            var db = this.Collection("klt.vanban");
            db.AddFilter("hoso_id", ObjectId.Parse(request.Form["_id"]));
            var data = await db.GetAll<VanBanModel>();
            return Ok(new { status = 1, payload = data });
        }

        [HttpPost]
        [Route("store-data")]
        public async Task<IHttpActionResult> StoreData()
        {
            var request = HttpContext.Current.Request;
            var files = request.Files.GetMultiple("datafiles[]");
            BsonArray dataFilesArray = new BsonArray();
            foreach (var file in files)
            {
                var fileName = file.FileName;
                var mime = file.ContentType;
                var size = file.ContentLength;
                var dataFileId = await this.StartForLoop()
                                        .AddOptionsMetadata("contentType", file.ContentType)
                                        .AddOptionsMetadata("filecode", request.Form["filecode"])
                                        .AddOptionsMetadata("title", request.Form["title"])
                                        .UploadFileGFSBucket(file);
                var docFile = new BsonDocument
                    {
                         {"filename", fileName },
                         {"mime" ,  mime},
                         {"size" ,  size},
                         {"data", dataFileId.ToString()}
                    };
                dataFilesArray.Add(docFile);
            }
            var db = this.Collection("klt.vanban");
            db.Add("filecode", request.Form["filecode"]);
            db.Add("title", request.Form["title"]);
            db.Add("description", request.Form["description"]);
            db.Add("hoso_id", new ObjectId(request.Form["hoso_id"]));
            db.Add("files", dataFilesArray);
            db.Add("created_at", DateTime.Now);
            db.Add("updated_at", DateTime.Now);
            await db.Store();
            return Ok(new { status = 1 });
        }

        [HttpPost]
        [Route("update")]
        public async Task<IHttpActionResult> Update()
        {
            var request = HttpContext.Current.Request;
            var db = this.Collection("klt.vanban");
            var filter = new BsonDocument
            {
                { "_id", ObjectId.Parse(request.Form["_id"])}
            };

            var database = this.connectDatabseMongo();
            var bucket = new GridFSBucket(database);
            BsonArray dataFilesArray = new BsonArray();

            var document = await db.Get<VanBanModel>(filter);
            var filesTemp = document.files;

            string fileOldsStr = request.Form["dataFileOld"];
            string[] fileOlds = fileOldsStr.Split('@');
            foreach (var file in filesTemp)
            {
                var flag = false;

                foreach (var fileOle in fileOlds)
                {
                    if (fileOle == file.filename)
                    {
                        flag = true;
                        BsonDocument docFile = new BsonDocument
                        {
                             {"filename", file.filename },
                                {"mime" ,  file.mime},
                             {"size" ,  file.size},
                             {"data", file.data}
                        };
                        dataFilesArray.Add(docFile);
                    }
                }
                if (!flag)
                {
                    await bucket.DeleteAsync(new ObjectId(file.data));
                }
            }

            var files = request.Files.GetMultiple("datafiles[]");
            foreach (var file in files)
            {
                var fileName = file.FileName;
                var mime = file.ContentType;
                var size = file.ContentLength;
                var dataFileId = await this
                                        .AddOptionsMetadata("contentType", file.ContentType)
                                        .AddOptionsMetadata("filecode", request.Form["filecode"])
                                        .AddOptionsMetadata("title", request.Form["title"])
                                        .UploadFileGFSBucket(file);
                var docFile = new BsonDocument
                    {
                         {"filename", fileName },
                         {"mime" ,  mime},
                         {"size" ,  size},
                         {"data", dataFileId.ToString()}
                    };
                dataFilesArray.Add(docFile);
            }

            db.Add("filecode", request.Form["filecode"]);
            db.Add("title", request.Form["title"]);
            db.Add("description", request.Form["description"]);
            db.Add("hoso_id", new ObjectId(request.Form["hoso_id"]));
            db.Add("files", dataFilesArray);
            db.Add("updated_at", DateTime.Now);
            await db.Update(filter);
            return Ok(new { status = 1 });
        }

        [HttpPost]
        [Route("delete")]
        public async Task<IHttpActionResult> Delete()
        {
            var request = HttpContext.Current.Request;
            var db = this.Collection("klt.vanban");

            var database = this.connectDatabseMongo();
            var bucket = new GridFSBucket(database);

            var filter = new BsonDocument
            {
                { "_id", ObjectId.Parse(request.Form["_id"])}
            };
            var document = await db.Get<VanBanModel>(filter);
            var filesTemp = document.files;
            foreach (var file in filesTemp)
            {

                await bucket.DeleteAsync(new ObjectId(file.data));
            }
            db.Delete(filter);
            return Ok(new { status = 1 });
        }
    }
}