using Newtonsoft.Json;
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
            Console.WriteLine("Connecting to WebSocket");
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
            Console.WriteLine($"Sending: {data}");
            _idToSend++;
            _socket.Send(data);
        }

        public void Unsubscribe(string unsubscribeOnTradingPair) {
            string data = $"{{\"method\": \"UNSUBSCRIBE\",\"params\": [{unsubscribeOnTradingPair}],\"id\": {_idToSend}}}";
            Console.WriteLine($"Sending: {data}");
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
                    Console.WriteLine($"server send me error: {e.Message}");
                } 
                else if (e.Message.Contains("\"result\":null")) { // subscribe or unsubscribe sucsessful.
                
                    var reg = Regex.Match(e.Message, @"\""id\"":\s?(\d+)", RegexOptions.IgnoreCase);
                    if (reg.Success)
                    {
                        string id = reg.Groups[1].Value;
                        Console.WriteLine($"Sucsessful sub/unsub. to id: {id}");
                        //var itemToRemove = waitForId.Single(r => r == Int32.Parse(id));
                    }
                    else {
                        Console.WriteLine("Sucsessful sub/unsub.");
                    }
                }
                else {
                    Console.WriteLine($"Unknown message: {e.Message}");
                }

            }
            catch (Exception ex) { Console.WriteLine($"ERROR: {ex.StackTrace}"); Console.WriteLine($"Data with error: {e.Message}"); }
        }

        public void CloseWebSocketInstance() {
            Console.WriteLine("close");
            _socket.Close();
            Console.WriteLine("Closed");
        }

        

        private void SocketClosed(object sender, EventArgs e)
        {
            Console.WriteLine("SOCKET CLOSED");

            //Thread.Sleep(4000);
            //Console.WriteLine("Reconnecting...");
            //this.InitSocket();
        }

        private void SocketError(object sender, SuperSocket.ClientEngine.ErrorEventArgs e)
        {
            Console.WriteLine("SOCKET ERROR: ");
            Console.WriteLine("e.Exception.Message: " + e.Exception.Message);

            if (KillOnError) {
                Console.WriteLine("Kill on Error Enabled ...-> Kill this pod.");
                Microsoft.Extensions.Diagnostics.HealthChecks.WssHealthCheck.healthy = false;
            }
        }

        private void SocketOpened(object sender, EventArgs e)
        {
            Console.WriteLine("SOCKET Connected");
        }

        private void SetTimer()
        {
            Console.WriteLine("Set Timer");
            _timer = new System.Timers.Timer();
            _timer.Interval = 60000;
            _timer.Elapsed += OnTimedEvent;
            _timer.Start();
        }

        public void SendPong()
        {
            this._socket.Send("pong");
        }

        private void OnTimedEvent(Object source, System.Timers.ElapsedEventArgs e)
        {
            Console.WriteLine("Sending pong");
            SendPong();
        }
    }
}
