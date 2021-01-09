using Serilog;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace Ros4
{
    public static class Data
    {
        public static string sqlConnStr { get; set; }
        public static string xurl { get; set; }

        public static List<PriceAlartRow> GetPriceAlartTable() {
            List<PriceAlartRow> priceAlartTable = new List<PriceAlartRow>();

            SqlConnection conn = new SqlConnection(sqlConnStr);
            string sql = "SELECT * from PriceAlart";
            SqlCommand cmd = new SqlCommand(sql, conn);
            conn.Open();
            try
            {
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    priceAlartTable.Add(new PriceAlartRow((IDataRecord)reader));
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "GetPriceAlartTable");
            }
            finally{
                conn.Close();
            }
            return priceAlartTable;
        }
        public static DateTime? GetSysDateTime()
        {
            SqlConnection conn = new SqlConnection(sqlConnStr);
            SqlCommand cmd = new SqlCommand("SELECT SYSDATETIME();", conn);
            conn.Open();
            try
            {
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    string datetime = reader[0].ToString();
                    var n = DateTime.Parse(datetime);
                    conn.Close();

                    return n;
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "GetSysDateTime");
            }
            finally
            {
                conn.Close();
            }
            return null;
        }

        public static void UpdateLastPrice(List<PriceAlartRow> updatedRows, bool onlyMark = false) {

            if (updatedRows == null || updatedRows.Count == 0) {
                return;
            }

            SqlConnection conn = new SqlConnection(sqlConnStr);
            conn.Open();
            
            try
            {
                foreach (PriceAlartRow row in updatedRows)
                {
                    if (row.last_price == null && !onlyMark) {
                        continue;
                    }
                    int id = row.id;
                    SqlCommand cmd = new SqlCommand($"UPDATE PriceAlart SET {(onlyMark?"":$"last_price = {row.last_price}, ")}pod_name = '{Core.POD_UUID}' WHERE id = {row.id};", conn);
                    int numAffectedRows = cmd.ExecuteNonQuery();
                    if (numAffectedRows > 0)
                    {
                        // all ok.
                    }
                    else {
                        Log.Warning($"UpdateLastPrice numAffectedRows? SQL:{cmd.CommandText}");
                        // todo print all rows to see where is error ....
                        // tuki je lahko tud da miu je kera druga mikorstoritev zbrisala vrstico v tabeli. !!!!
                        // torej ignire za enkrat.
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "UpdateLastPrice");
            }
            finally
            {
                conn.Close();
            }
        }
        public static void DeleteFromPriceAlart(List<PriceAlartRow> deleteRows) {

            if (deleteRows == null || deleteRows.Count == 0)
            {
                //Console.WriteLine("No rows to update.");
                return;
            }

            SqlConnection conn = new SqlConnection(sqlConnStr);
            conn.Open();

            try
            {
                foreach (PriceAlartRow row in deleteRows)
                {
                    int id = row.id;
                    SqlCommand cmd = new SqlCommand($"DELETE FROM PriceAlart WHERE id = {id};", conn);
                    int numAffectedRows = cmd.ExecuteNonQuery();
                    if (numAffectedRows > 0)
                    {
                        // all ok.
                        //deleteRows.Remove(deleteRows.Single(x => x.id == id));
                    }
                    else
                    {
                        Log.Error($"DeleteFromPriceAlart numAffectedRows? SQL:{cmd.CommandText}");
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "DeleteFromPriceAlart");
            }
            finally
            {
                conn.Close();
            }
        }

        public static bool DeleteFromPriceAlartById(int id)
        {
            Log.Information($"Delete By id:{id}");
            SqlConnection conn = new SqlConnection(sqlConnStr);
            conn.Open();

            try
            {
                SqlCommand cmd = new SqlCommand($"DELETE FROM PriceAlart WHERE id = {id};", conn);
                int numAffectedRows = cmd.ExecuteNonQuery();
                if (numAffectedRows > 0)
                {
                    conn.Close();
                    return true;
                }
                else
                {
                    Log.Error($"DeleteFromPriceAlartById numAffectedRows? SQL:{cmd.CommandText}");
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "DeleteFromPriceAlartById");
            }
            finally
            {
                conn.Close();
            }

            return false;
        }

        public static bool InsertPriceAlart(string tradinPair, decimal price, bool oneTime, string mobi, string mail)
        {
            SqlConnection conn = new SqlConnection(sqlConnStr);
            conn.Open();

            try
            {
                SqlCommand cmd = new SqlCommand($"INSERT INTO PriceAlart (trading_pair, price, one_time, mobi, email) VALUES(@tr, @price, @ot, @mobi, @mail);", conn);
                cmd.Parameters.AddWithValue("@tr", tradinPair.ToLower());
                cmd.Parameters.AddWithValue("@price", price);
                cmd.Parameters.AddWithValue("@ot", oneTime?1:0);
                cmd.Parameters.AddWithValue("@mobi", mobi);
                cmd.Parameters.AddWithValue("@mail", mail.ToLower());
                int numAffectedRows = cmd.ExecuteNonQuery();
                if (numAffectedRows > 0)
                {
                    conn.Close();
                    return true;
                }
                else
                {
                    Log.Error($"InsertPriceAlart numAffectedRows? SQL:{cmd.CommandText}");
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "InsertPriceAlart");
            }
            finally
            {
                conn.Close();
            }

            return false;
        }
    }
}
