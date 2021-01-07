using System;
using System.Collections.Generic;
using System.Linq;
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
            _timerUpdateDB.Interval = rand.Next(6000, 9000);
            //watchedTradingPairs.RemoveAll(x => x.last_price == null);
            List<PriceAlartRow> newList = new List<PriceAlartRow>(watchedTradingPairs);
            try
            {
                for (int j = 0; j < Kline.p.Count; j++)
                {
                    bool b = false;
                    for (int i = 0; i < newList.Count; i++)
                    {
                        if (b) {
                            newList.RemoveAt(i);
                            continue;
                        }
                        if (Kline.p.ElementAt(j).Key == newList.ElementAt(i).trading_pair)
                        {
                            newList.ElementAt(i).last_price = Kline.p.ElementAt(j).Value;
                            b = true;
                        }
                    }
                }
                Data.UpdateLastPrice(newList);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
        private void OnTimedEventSelectDB(object sender, ElapsedEventArgs e)
        {
            _timerUpdateDB.Interval = rand.Next(10000, 14000);

            List<PriceAlartRow> priceAlartTable = Data.GetPriceAlartTable();
            DateTime? sysDateTime = Data.GetSysDateTime();

            if ( ! sysDateTime.HasValue) {
                Console.WriteLine("No SQL system DATETIME !!!");
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
