using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WebSocket4Net;

namespace Ros4
{
    public class Kline
    {
        private System.Timers.Timer _timer; 
        public WebSocket _socket = null;
        private int _idToSend = 1;
        private List<string> tradingPairSubscriptions = new List<string>();
        public static IDictionary<string, decimal> p = new Dictionary<string, decimal>();
        public static string endpoint { get; set; }
        public static bool KillOnError { get; set; }


        public Kline()
        {
            Log.Information("Connecting to WebSocket");
            InitSocket(endpoint);
        }

        private void InitSocket(string endpoint)
        {
            this._socket = new WebSocket(endpoint);

            this._socket.Opened += SocketOpened;
            this._socket.Error += SocketError;
            this._socket.Closed += SocketClosed;
            this._socket.MessageReceived += SocketMessageReceived;
            this._socket.Open();
        }

        public void SubscribeKline(string tradingPair) {
            tradingPairSubscriptions.Add(tradingPair.ToLower());
            Subscribe($"\"{tradingPair.ToLower()}@kline_1m\"");
        }

        public void UnsubscribeKline(string tradingPair)
        {
            tradingPairSubscriptions.Remove(tradingPair.ToLower());
            Unsubscribe($"\"{tradingPair.ToLower()}@kline_1m\"");
        }

        public void Subscribe(string subscribeOnTradingPair) {
            string data = $"{{\"method\": \"SUBSCRIBE\",\"params\": [{subscribeOnTradingPair}],\"id\": {_idToSend}}}";
            Log.Information($"Sending: {data}");
            _idToSend++;
            _socket.Send(data);
        }

        public void Unsubscribe(string unsubscribeOnTradingPair) {
            string data = $"{{\"method\": \"UNSUBSCRIBE\",\"params\": [{unsubscribeOnTradingPair}],\"id\": {_idToSend}}}";
            Log.Information($"Sending: {data}");
            _idToSend++;
            _socket.Send(data);
        }

        private void SocketMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            try
            {
                //Console.WriteLine($"Data: {e.Message}");

                if (e.Message.Contains("\"e\":\"kline\""))
                {
                    string pattern = @"\""c\"":\""((\d|\.)+)\""";
                    var reg = Regex.Match(e.Message, pattern, RegexOptions.IgnoreCase);
                    if (reg.Success)
                    {
                        string newPrice = reg.Groups[1].Value;
                        //Console.WriteLine($"New price: {newPrice}");

                        var reg1 = Regex.Match(e.Message, @"\""s\"":\s?\""([A-z0-9]+)\""", RegexOptions.IgnoreCase);
                        if (reg1.Success)
                        {
                            string tradingPair = reg1.Groups[1].Value.ToLower();
                            //Console.WriteLine($"New price: {newPrice} form trading pair: {tradingPair}");
                            if (p.ContainsKey(tradingPair))
                            {
                                p[tradingPair] = decimal.Parse(newPrice);
                            }
                            else {
                                p.Add(tradingPair, decimal.Parse(newPrice));
                            }
                        }
                    }
                }
                else if (e.Message.Contains("\"error\""))
                {
                    Log.Error($"server send me error: {e.Message}");
                } 
                else if (e.Message.Contains("\"result\":null")) { // subscribe or unsubscribe sucsessful.
                
                    var reg = Regex.Match(e.Message, @"\""id\"":\s?(\d+)", RegexOptions.IgnoreCase);
                    if (reg.Success)
                    {
                        string id = reg.Groups[1].Value;
                        Log.Information($"Sucsessful sub/unsub. to id: {id}");
                        //var itemToRemove = waitForId.Single(r => r == Int32.Parse(id));
                    }
                    else {
                        Log.Information("Sucsessful sub/unsub.");
                    }
                }
                else {
                    Log.Information($"Unknown message: {e.Message}");
                }

            }
            catch (Exception ex) { Log.Error(ex, $"SocketMessageReceived Message:{e.Message}"); }
        }

        public void CloseWebSocketInstance() {
            Log.Information("close");
            _socket.Close();
            Log.Information("Closed");
        }

        

        private void SocketClosed(object sender, EventArgs e)
        {
            Log.Information("SOCKET CLOSED");

            //Thread.Sleep(4000);
            //Console.WriteLine("Reconnecting...");
            //this.InitSocket();
        }

        private void SocketError(object sender, SuperSocket.ClientEngine.ErrorEventArgs e)
        {
            Log.Error(e.Exception, "SocketError event");

            if (KillOnError) {
                Log.Information("Kill on Error Enabled ...-> Kill this pod.");
                Microsoft.Extensions.Diagnostics.HealthChecks.WssHealthCheck.healthy = false;
            }
        }

        private void SocketOpened(object sender, EventArgs e)
        {
            Log.Information("SOCKET Connected");
        }
    }
}
