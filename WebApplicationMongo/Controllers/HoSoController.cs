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
using WebApplicationMongo.Models;

namespace WebApplicationMongo.Controllers
{
    [RoutePrefix("hoso")]
    public class HoSoController : MainController
    {
        [HttpGet]
        [Route("getlist")]
        public async Task<IHttpActionResult> GetList()
        {
            var request = HttpContext.Current.Request;
            var db = this.Collection("klt.hoso");
            var data = await db.GetAll<HoSoModel>();
            return Ok(new { status = 1, payload = data });
        }
        [HttpPost]
        [Route("show")]
        public async Task<IHttpActionResult> Show()
        {
            var request = HttpContext.Current.Request;
            var db = this.Collection("klt.hoso");
            var filter = new BsonDocument
            {
                { "_id", ObjectId.Parse(request.Form["_id"])}
            };
            var data = await db.Get<HoSoModel>(filter);
            return Ok(new { status = 1, payload = data });
        }

        [HttpGet]
        [Route("search")]
        public async Task<IHttpActionResult> Search()
        {
            var request = HttpContext.Current.Request;
            string search = request.QueryString["search"];
            var db = this.Collection("klt.hoso");

            db.AddRelation("klt.vanban", "_id", "hoso_id", "vanban");
            var filter1 = new BsonDocument("title", new BsonRegularExpression(@"/" + search + "/i"));
            var filter2 = new BsonDocument("vanban.title", new BsonRegularExpression(@"/" + search + "/i"));
            var filterOr = new BsonArray();
            filterOr.Add(filter1);
            filterOr.Add(filter2);
            db.AddOrFilter(filterOr);
            //db.AddFilter("title", new BsonRegularExpression(@"/" + search + "/i"));
            //db.AddFilter("vanban.title", new BsonRegularExpression(@"/" + search + "/i"));
            db.AddMatch();

            var data = await db.S_Aggregate<HoSoModel>();
            return Ok(new { status = 1, payload = data });
        }

        [HttpPost]
        [Route("store-data")]
        public async Task<IHttpActionResult> StoreData()
        {
            var request = HttpContext.Current.Request;
            var db = this.Collection("klt.hoso");
            db.Add("filecode", request.Form["filecode"]);
            db.Add("title", request.Form["title"]);
            db.Add("description", request.Form["description"]);
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
            var db = this.Collection("klt.hoso");
            var filter = new BsonDocument
            {
                { "_id", ObjectId.Parse(request.Form["_id"])}
            };
            db.Add("filecode", request.Form["filecode"])
                .Add("title", request.Form["title"])
                .Add("description", request.Form["description"])
                .Add("updated_at", DateTime.Now);
            await db.Update(filter);
            return Ok(new { status = 1 });
        }

        [HttpPost]
        [Route("delete")]
        public async Task<IHttpActionResult> Delete()
        {
            var request = HttpContext.Current.Request;
            var db = this.Collection("klt.hoso");
            var filter = new BsonDocument
            {
                { "_id", ObjectId.Parse(request.Form["_id"])}
            };
            db.Delete(filter);
            return Ok(new { status = 1 });
        }

    }
}