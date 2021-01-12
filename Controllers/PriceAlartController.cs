using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Ros4.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class PriceAlartController : Controller
    {
        // GET: PriceAlartController
        [HttpGet]
        public IEnumerable<PriceAlartRow> Get() {
            string hostIP = Request.Host.Value;

            Log.Information($"Host info: {hostIP}");
            if (hostIP.StartsWith("10.") || hostIP.StartsWith("192.168.") || hostIP.Contains("localhost"))
            {

            }
            else {
                if (hostIP.Split('.').Length == 4) { 
                    if (Core.hostIP != hostIP { 
                        Log.Information("Setting xurl IP!");
                        Core.hostIP = hostIP;
                    }
                }
                
            }

            var n = Data.GetPriceAlartTable();
            return n.ToArray();
        }


        [HttpPost]
        public ActionResult<bool> Post(IFormCollection collection)
        {
            var lines = collection.Select(kvp => kvp.Key + ": " + kvp.Value.ToString());
            Log.Information("POST2: " + string.Join(Environment.NewLine, lines));
            try
            {
                Data.InsertPriceAlart(collection["trading_pair"], decimal.Parse(collection["price"]), collection["times"]=="onetime", collection["sms"], collection["mail"]);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Post ... InsertPriceAlart");
                return false;
            }
            return true;
        }


        // DELETE: PriceAlart/5
        [HttpDelete("{id}")]
        public ActionResult<bool> Delete(int id)
        {
            return Data.DeleteFromPriceAlartById(id);
        }
    }
}
