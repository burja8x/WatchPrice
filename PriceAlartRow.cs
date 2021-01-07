using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Ros4
{
    public class PriceAlartRow
    {
        public int id { get; set; }
        public string trading_pair { get; set; }
        public decimal price { get; set; }
        public bool one_time { get; set; }
        public string mobi { get; set; }
        public string email { get; set; }
        public string pod_name { get; set; }
        public decimal? last_price { get; set; }
        public DateTime? last_update { get; set; }

        public PriceAlartRow(IDataRecord record) {
            //Console.WriteLine(String.Format("{0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}", record[0], record[1], record[2], record[3], record[4], record[5], record[6], record[7], record[8]));
            id = (int)record[0];
            trading_pair = (string)record[1];
            price = (decimal)record[2];
            one_time = (bool)record[3];
            mobi = (string)(record.IsDBNull(4) ? "" : record.GetValue(4));
            email = (string)(record.IsDBNull(5) ? "" : record.GetValue(5));
            pod_name = (string)(record.IsDBNull(6) ? "" : record.GetValue(6));
            last_price = (decimal)(record.IsDBNull(7) ? default(decimal) : record.GetValue(7));
            last_update = (DateTime)(record.IsDBNull(8) ? default(DateTime) : record.GetValue(8));
        }
    }
}
