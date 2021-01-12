using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Timers;

namespace Ros4
{
    public class Core
    {
        // skrbi za to da vse deluje kot mora.
        private Timer _timerUpdateDB = new Timer();
        private Timer _timerSelectDB = new Timer();
        private Random rand = new Random();
        public static Kline binanceWS;
        private List<PriceAlartRow> watchedTradingPairs = new List<PriceAlartRow>();
        private const int limit = 10;
        public static string POD_UUID;
        public static string hostIP = "";
        private List<string> removeFromP = new List<string>();

        public Core() {
            
            string? pod_uid = Environment.GetEnvironmentVariable("MY_POD_UID");
            if (pod_uid == null)
            {
                POD_UUID = "DOCKER_DEV_" + rand.Next().ToString();
            }
            else {
                POD_UUID = pod_uid;
            }

            binanceWS = new Kline();
            SetupTimers();
        }


        private void SetupTimers() {

            _timerUpdateDB = new Timer();
            _timerUpdateDB.Interval = 20000;
            _timerUpdateDB.Elapsed += OnTimedEventUpdateDB;
            _timerUpdateDB.Start();

            _timerSelectDB = new Timer();
            _timerSelectDB.Interval = 10000;
            _timerSelectDB.Elapsed += OnTimedEventSelectDB;
            _timerSelectDB.Start();
        }

        private void OnTimedEventUpdateDB(object sender, ElapsedEventArgs e)
        {
            _timerUpdateDB.Interval = rand.Next(8000, 9000);
            //watchedTradingPairs.RemoveAll(x => x.last_price == null);
            List<PriceAlartRow> newList = new List<PriceAlartRow>(watchedTradingPairs);
            try
            {
                for (int j = 0; j < Kline.p.Count; j++)
                {
                    for (int i = 0; i < newList.Count; i++)
                    {
                        if (Kline.p.ElementAt(j).Key == newList.ElementAt(i).trading_pair)
                        {
                            if (newList.ElementAt(i).last_price != null && (decimal)newList.ElementAt(i).last_price != 0 && 
                                IsBetween(newList.ElementAt(i).price, Math.Min((decimal)newList.ElementAt(i).last_price, Kline.p.ElementAt(j).Value), Math.Max((decimal)newList.ElementAt(i).last_price, Kline.p.ElementAt(j).Value))) {
                                Log.Information($"price crosses alart price..... sending... alart price:{newList.ElementAt(i).price}, prev price:{(decimal)newList.ElementAt(i).last_price}, now price:{Kline.p.ElementAt(j).Value}");
                                Data.DeleteFromPriceAlartById(newList.ElementAt(i).id);

                                // SEND
                                List<KeyValuePair<string, string>> list = new List<KeyValuePair<string, string>>();
                                list.Add(new KeyValuePair<string, string>("content", $"Price ALART: {newList.ElementAt(i).trading_pair.ToUpper()} price crosses: {newList.ElementAt(i).price}"));
                                list.Add(new KeyValuePair<string, string>("sms", newList.ElementAt(i).mobi));
                                list.Add(new KeyValuePair<string, string>("mail", newList.ElementAt(i).email));
                                newList.RemoveAt(i);
                                try
                                {
                                    Log.Information($"Sending POST request to REPORT micro... DATA:{string.Join("   ", list.Select(kvp => kvp.Key + ":" + kvp.Value.ToString()))}  URL: {Data.xurl}");

                                    string url = hostIP.Length == 0 ? Data.xurl : "http://" + hostIP + "/report/api/v1/report";

                                    string result = PostWithAuthorization(Data.xurl, list.ToArray()).Result;
                                }
                                catch (Exception ex)
                                {
                                    Log.Error(ex, "OnTimedEventUpdateDB ... sending SMS");
                                }

                                break;
                            }
                            newList.ElementAt(i).last_price = Kline.p.ElementAt(j).Value;
                        }
                    }
                }
                Data.UpdateLastPrice(newList);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "OnTimedEventUpdateDB");
            }
        }

        public async Task<string> PostWithAuthorization(string link, KeyValuePair<string, string>[] contents)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    var postData = new FormUrlEncodedContent(contents);

                    client.Timeout = TimeSpan.FromSeconds(5);
                    var result = await client.PostAsync(link, postData);
                    return await result.Content.ReadAsStringAsync();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "PostWithAuthorization");
            }
            return "OK";
        }

        private void OnTimedEventSelectDB(object sender, ElapsedEventArgs e)
        {
            _timerSelectDB.Interval = rand.Next(12000, 14000);

            List<PriceAlartRow> priceAlartTable = Data.GetPriceAlartTable();
            DateTime? sysDateTime = Data.GetSysDateTime();

            if ( ! sysDateTime.HasValue) {
                Log.Error("No SQL system DATETIME !!!");
                return;
            }

            foreach (string trading_p in removeFromP)
            {
                Kline.p.Remove(trading_p);
            }

            // unsubscribe če je bil odstranjen iz baze.
            List<PriceAlartRow> newList = new List<PriceAlartRow>(watchedTradingPairs);
            foreach (PriceAlartRow row in priceAlartTable)
            {
                if (newList.Exists(w => w.id == row.id)) {
                    newList.RemoveAll(x => x.id == row.id);
                }
            }
            if (newList.Count > 0) {
                if (binanceWS._socket.State == WebSocket4Net.WebSocketState.Open)
                {
                    foreach (PriceAlartRow item in newList)
                    {
                        watchedTradingPairs.RemoveAll(x => x.id == item.id);
                    }
                    binanceWS.Unsubscribe("\"" + String.Join("@kline_1m\",\"", newList.Select(x => x.trading_pair)) + "@kline_1m\"");
                }
            }
            removeFromP = new List<string>(newList.Select(x => x.trading_pair));


            // če noben ni updated cene v bazi 30 sekund ga prevzeme ta pod (samo v premeru je ima prosta mesta {limit})
            List<PriceAlartRow> addTradingPairs = new List<PriceAlartRow>();
            
            foreach (PriceAlartRow row in priceAlartTable)
            {
                if (watchedTradingPairs.Exists(w => w.id == row.id)) {
                    continue;
                }
                
                if (priceAlartTable.Count == limit) {
                    // TODO mark as my.
                    // TODO run new microservice.
                    break; // samo 10 jih lahko imamo na enkrat .... seveda jih je lahko več to je za demo.
                }


                if (!row.last_update.HasValue) {
                    // Websocket ADD
                    addTradingPairs.Add(row);
                }
                else if (!IsBetween<long>(Math.Abs(row.last_update.Value.Ticks - sysDateTime.Value.Ticks), 0, 100000000)) { //30s
                    // Websocket ADD
                    addTradingPairs.Add(row);
                }
                else {
                    // all ok.
                }
            }
            Data.UpdateLastPrice(addTradingPairs, true);

            WatchTradingPairs(addTradingPairs);
        }
        public static bool IsBetween<T>(T item, T start, T end)
        {
            return Comparer<T>.Default.Compare(item, start) >= 0
                && Comparer<T>.Default.Compare(item, end) <= 0;
        }

        public void WatchTradingPairs(List<PriceAlartRow> addTradingPairs) {
            if (addTradingPairs.Count == 0) {
                return;
            }
            if (binanceWS._socket.State == WebSocket4Net.WebSocketState.Open)
            {
                binanceWS.Subscribe("\"" + String.Join("@kline_1m\",\"", addTradingPairs.Select(x => x.trading_pair)) + "@kline_1m\"");
                watchedTradingPairs.AddRange(addTradingPairs);
            }
        }

    }
}
