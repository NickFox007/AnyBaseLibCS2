﻿using System;
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

namespace AnyBaseLib.Bases
{
    internal static class Common
    {       
        private static string logpath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),"queries.txt");
        public static string _PrepareClear(string q, List<string> args)
        {
            var new_q = q;
            if(args != null) foreach (var arg in args)
            {
                var regex = new Regex(Regex.Escape("{ARG}"));
                var new_q2 = regex.Replace(new_q, _PrepareArg(arg), 1);
                if (new_q2 == new_q) throw new Exception("Mailformed query [Too many args]");
                new_q = new_q2;
            }
            if (new_q.Contains("{ARG}")) throw new Exception("Mailformed query [Not enough args]");
            return new_q;
        }

        public static string _PrepareArg(string arg)
        {
            var new_arg = arg;
            string[] escapes = ["'", "\"", "`", "%", "-", "_"];

            foreach (var escape in escapes)
            {
                new_arg = new_arg.Replace(escape, $"\\{escape}");
            }
            return new_arg;
        }

        
        public static List<List<string>> _Query(DbConnection conn, string q, bool non_query)
        {
            File.AppendAllText(logpath, $"[{DateTime.Now}] {q} (Non-query: {non_query})\n");
            var sql = conn.CreateCommand();
            sql.CommandText = q;
            //Console.WriteLine($"Query: {q} [Non-query: {non_query}]");
            if (!non_query)
            {
                var list = new List<List<string>>();

                using (var reader = sql.ExecuteReader())
                {
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

        public static void QueryAsync(DbConnection conn, string q, Action<List<List<string>>> action = null, bool non_query = false)
        {
            var task = new Task<List<List<string>>>(() => Query(conn, q, non_query));
            task.Start();
            if (action != null) task.ContinueWith((obj) => action(obj.Result));            
        }

        /*
        public static void QueryDapperAsync(DbConnection conn, Type type, string q, Action<object> action = null, bool non_query = false)
        {
            var task = new Task<object>(() => QueryDapper(conn, type, q, non_query));
            task.Start();
            if (action != null) task.ContinueWith((obj) => action(obj.Result));
        }
        */
        public static List<List<string>> Query(DbConnection conn, string q, bool non_query = false)
        {
            try
            {
                return Common._Query(conn, q, non_query);
            }

            catch (Exception e)
            {
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
