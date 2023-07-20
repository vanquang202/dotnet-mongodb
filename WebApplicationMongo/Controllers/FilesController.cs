using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using MongoDB.Driver.GridFS;
using Newtonsoft.Json;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.IO;

namespace WebApplicationMongo.Controllers
{
    [RoutePrefix("files")]
    public class FilesController : MainController
    {
        [HttpPost]
        [Route("show")]
        public async Task<IHttpActionResult> Show()
        {
            var request = HttpContext.Current.Request;
            var base64String = await this.GetBaseStringByFileID(new ObjectId(request.Form["_id"]));
            return Ok(new { status = 1, payload = base64String });
        }

        [HttpGet]
        [Route("download")]
        public async Task<HttpResponseMessage> Download(string id)
        {
            var database = this.connectDatabseMongo();
            var fileId = new ObjectId(id);

            var gridFSBucket = new GridFSBucket(database);
            var downloadStream = await gridFSBucket.OpenDownloadStreamAsync(fileId);
            var fileMetadata = downloadStream.FileInfo;

            var contentType = fileMetadata.Metadata.GetElement("contentType").Value.ToString();
            var fileName = fileMetadata.Filename;
            var result = new HttpResponseMessage(HttpStatusCode.OK);
            result.Content = new StreamContent(downloadStream);
            result.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
            result.Content.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("attachment");
            result.Content.Headers.ContentDisposition.FileName = fileName;
            return result;
        }
    }
}