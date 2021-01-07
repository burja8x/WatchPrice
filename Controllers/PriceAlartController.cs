﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
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
            var n = Data.GetPriceAlartTable();
            return n.ToArray();
        }


        [HttpPost]
        public ActionResult<bool> Post(IFormCollection collection)
        {
            Console.WriteLine("POST !!!!");
            Console.WriteLine(collection.Count);
            try
            {
                Data.InsertPriceAlart(collection["trading_pair"], decimal.Parse(collection["price"]), collection["times"]=="onetime", collection["sms"], collection["mail"]);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
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