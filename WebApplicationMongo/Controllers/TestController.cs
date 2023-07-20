using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using WebApplicationMongo.Models;

namespace WebApplicationMongo.Controllers
{
    [RoutePrefix("test")]
    public class TestController : MainController
    {

        [HttpGet]
        [Route("getlist")]
        public async Task<IHttpActionResult> GetList()
        {
            var request = HttpContext.Current.Request;
            var db = this.Collection("test");
            var data = await db.GetAll<TestModel>();
            return Ok(new { status = 1, payload = data });
        }

        [HttpPost]
        [Route("store-data")]
        public async Task<IHttpActionResult> StoreData()
        {
            var request = HttpContext.Current.Request;
            var db = this.Collection("test");
            db.Add("filecode", request.Form["filecode"]);
            db.Add("title", request.Form["title"]);
            db.Add("description", request.Form["description"]);
            db.Add("created_at", DateTime.Now);
            db.Add("updated_at", DateTime.Now);
            await db.Store();
            return Ok(new { status = 1 });
        }
    }
}