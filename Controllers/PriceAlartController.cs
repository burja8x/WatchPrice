using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
            var n = Data.GetPriceAlartTable();
            //try
            //{
            //    WebClient web = new WebClient();

            //    string result = web.DownloadString(Data.xurl);
            //    Console.Write($"Respone WWWW api:{result}.");
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine(ex);
            //}

            List<KeyValuePair<string, string>> list = new List<KeyValuePair<string, string>>();
            //list.Add(new KeyValuePair<string, string>("Content-Type", "multipart/form-data"));
            list.Add(new KeyValuePair<string, string>("content", "Hello"));
            list.Add(new KeyValuePair<string, string>("sms", "031841816"));
            list.Add(new KeyValuePair<string, string>("mail", ""));
            string result = PostWithAuthorization(Data.xurl, list.ToArray()).Result;
            Console.WriteLine(result);

            return n.ToArray();
        }

        public async Task<string> PostWithAuthorization(string link, KeyValuePair<string, string>[] contents)
        {
            
            try
            {
                using (var client = new HttpClient())
                {
                    //HttpContent httpContent = new StringContent(contents, Encoding.UTF8);
                    //httpContent.Headers.ContentType = new MediaTypeHeaderValue("multipart/form-data");
                    //var neki = new StringContent("", Encoding.UTF8, "multipart/form-data");
                    //client.;
                    //client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("multipart/form-data"));
                    //HttpStringContent httpStringContent = new HttpStringContent(param, Encoding.UTF8, "multipart/form-data");
                    //client.DefaultRequestHeaders.Add("Content-Type", "multipart/form-data");
                    //client.DefaultRequestHeaders.Add(contents[1].Key, contents[1].Value);
                    //client.DefaultRequestHeaders.Add(contents[2].Key, contents[2].Value);
                    var postData = new FormUrlEncodedContent(contents);
                    
                    client.Timeout = TimeSpan.FromSeconds(10);
                    //client.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "multipart/form-data");
                    var result = await client.PostAsync(link, postData);
                    return await result.Content.ReadAsStringAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            return "OK";
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
