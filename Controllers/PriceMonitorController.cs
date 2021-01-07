using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ros4.Controllers
{
    [ApiController]
    [Route("/api/v1/[controller]")]
    public class PriceMonitorController : ControllerBase
    {
        [HttpGet]
        public IEnumerable<PriceInfo> Get()
        {
            List<PriceInfo> m = new List<PriceInfo>();
            for (int i = 0; i < Kline.p.Count; i++)
            {
                m.Add(new PriceInfo { TradingPair = Kline.p.ElementAt(i).Key.ToUpper(), LastPrice = Kline.p.ElementAt(i).Value });
            }
            return m.ToArray();
        }

        // TEST
        [HttpGet("{id}")]
        public ActionResult<string> Get(int id)
        {
            if (id == 1)
            {
                Core.binanceWS.SubscribeKline("btcusdt");
                return "btcusdt@kline_1m";
            }
            else if (id == 2)
            {
                Core.binanceWS.SubscribeKline("etcusdt");
                return "etcusdt@kline_1m";
            }
            else if (id == 3)
            {
                Core.binanceWS.SubscribeKline("ltcusdt");
                return "ltcusdt@kline_1m";
            }
            else if (id == 4) {
                Core.binanceWS.UnsubscribeKline("etcusdt");
                return "etcusdt@kline_1m U";
            }
            else if (id == 5) {
                Core.binanceWS.SubscribeKline("etffcusdt");
                return "etffcusdt@kline_1m";
            }
            else if (id == 6)
            {
                Core.binanceWS.UnsubscribeKline("etffcusdt");
                return "etffcusdt@kline_1m U";
            }
            else
            {
                return "null X";
            }
        }
    }
}
