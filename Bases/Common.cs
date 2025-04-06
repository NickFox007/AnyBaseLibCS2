using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Data.Common;
using Dapper;
using System.Runtime.CompilerServices;
using System.Reflection;
using System.Data;
using System.Reflection.Metadata.Ecma335;

namespace AnyBaseLib.Bases
{
    internal static class Common
    {
        //private static string logpath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),"queries.txt");
        private static string ReplaceFirst(this string text, string search, string replace)
        {
            int pos = text.IndexOf(search);
            if (pos < 0)
            {
                return text;
            }
            return text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
        }

        public static string _PrepareClear(string q, List<string> args)
        {
            var new_q = q;
            if(args != null) foreach (var arg in args.ToList())
            {
                var new_q2 = ReplaceFirst(new_q, "{ARG}", _PrepareArg(arg));

                if (new_q2 == new_q) throw new Exception("Mailformed query [Too many args in params]");
                new_q = new_q2;
            }
            if (new_q.Contains("{ARG}")) throw new Exception("Mailformed query [Not enough args in params]");
            return new_q;
        }

        public static string _PrepareArg(string arg)
        {
            if (arg == null) return "";
            
            var new_arg = arg;
            
            //string[] escapes = ["'", "\"", "`", "%", "-", "_"];
            string[] escapes = ["'", "\"", "`", "%", "\\"];

            foreach (var escape in escapes)
            {
                new_arg = new_arg.Replace(escape, $"\\{escape}");
            }
            
            return new_arg;
        }

        
        public static List<List<string>> _Query(DbConnection conn, string q, bool non_query)
        {
            if (conn.State != ConnectionState.Open) conn.Open();
            var sql = conn.CreateCommand();
            sql.CommandText = q;
            if (!non_query)
            {
                var list = new List<List<string>>();

                using (var readerAs = sql.ExecuteReaderAsync())
                {
                    var reader = readerAs.Result;
                    while (reader.Read())
                    {
                        var fields = new List<string>();

                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            if (!reader.IsDBNull(i))
                            {
                                fields.Add(reader.GetValue(i).ToString());
                            }
                            else
                                fields.Add(null);
                        }
                        list.Add(fields);
                    }
                }
                return list;
            }
            else
                sql.ExecuteNonQuery();
            return null;
        }


        public static bool Init(DbConnection conn, string name)
        {
            try
            {
                conn.Open();
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Cannot init {name} base: {e.Message}");
                return false;
            }
        }

        public static void QueryAsync(DbConnection conn, string q, Action<List<List<string>>> action = null, bool non_query = false, bool close = true)
        {
            int wait_opened = 10;
            while(wait_opened > 0)
            {
                if (conn.State == ConnectionState.Open)
                    break;
                wait_opened--;
                Task.Delay(150).Wait();
            }

            if (wait_opened == 0) throw new Exception("Error caused while open database connection");

            var task = new Task<List<List<string>>>(() => Query(conn, q, non_query, close));
            if (action != null) task.ContinueWith(obj => action(obj.Result));
            task.Start();

        }

        public static List<List<string>> Query(DbConnection conn, string q, bool non_query = false, bool close = false)
        {
            //Console.WriteLine("[ !!! DEBUG !!! ] Sync query . . .");
            try
            {

                var ret = _Query(conn, q, non_query);
                if (close) conn.Close();
                return ret;
                
            }

            catch (Exception e)
            {
                if (close) conn.Close();
                Console.WriteLine($"[Query] Error was caused while querying \"{q}\":\n{e.Message}\n\n{e.StackTrace}");
            }

            return null;
        }
        /*

        public static object QueryDapper(DbConnection conn, Type type, string q, bool non_query = false)
        {
            try
            {
                return Common._QueryDapper(conn, q, non_query);
            }

            catch (Exception e)
            {
                Console.WriteLine($"[QueryDapper] Error was caused while fetching query \"{q}\":\n{e.Message}");
            }
            return null;
        }*/

        /*
        public static object QueryDapper(DbConnection conn, string q, bool non_query)
        {
            try
            {
                return Common._Query2(conn, q, non_query);
            }

            catch (Exception e)
            {
                Console.WriteLine($"Error was caused while fetching query \"{q}\":\n{e.Message}");
            }
            return null;
        }
        */

    }
    public enum CommitMode
    {
        AutoCommit = 0,
        TimerCommit = 1,
        NoCommit = 2
    }
}
