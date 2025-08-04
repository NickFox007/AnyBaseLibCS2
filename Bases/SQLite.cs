using Microsoft.Data.Sqlite;
using System.Data.Common;

namespace AnyBaseLib.Bases
{
    internal class SQLiteDriver : IAnyBase
    {        
        
        private SqliteConnection dbConn;
        private CommitMode commit_mode;
        private bool trans_started = false;
        private DbTransaction transaction;

        public void Set(CommitMode commit_mode, string db_name, string db_host = "", string db_user = "", string db_pass = "")
        {
            this.commit_mode = commit_mode;
            dbConn = new SqliteConnection($"Data Source={db_name}.sqlite;");

            if (commit_mode != CommitMode.AutoCommit)
                new Task(TimerCommit).Start();
        }

        private void TimerCommit()
        {
            while (true)
            {
                if (trans_started)
                    SetTransState(false);
                Thread.Sleep(5000);
                //Task.Delay(5000);
            }
        }
        private string _FixForSQLite(string q)
        {
            return q.Replace("PRIMARY KEY AUTO_INCREMENT", "PRIMARY KEY AUTOINCREMENT").Replace("UNIX_TIMESTAMP()", "UNIXEPOCH()");
        }

        public static string _PrepareArg(string arg)
        {
            if (arg == null) return "";

            //var new_arg = arg;

            //string[] escapes = ["'", "\"", "`", "%", "-", "_"];
            string[] escapes = ["\\",  "`", "%"];
            //string[] escapes = ["\\","'", "`", "%"];



            //foreach (var escape in escapes)
            //{
            var new_arg = "";
            foreach (var ch in arg)
            {
                if (escapes.Contains(ch.ToString()))
                    new_arg += "\\";
                new_arg += ch.ToString();
            }
            //new_arg = new_arg.Replace(escape, $"\\{escape}");

            return new_arg.Replace("\"","\"\"").Replace("'","''");
        }

        public List<List<string>> Query(string q, List<string> args = null, bool non_query = false)
        {
            if(commit_mode != CommitMode.AutoCommit)
            {
                if(!trans_started && non_query) SetTransState(true);
                else
                {
                    if(trans_started && !non_query) SetTransState(false);
                }
            }    

            return Common.Query(dbConn, Common._PrepareClear(_FixForSQLite(q), args, _PrepareArg), non_query);
        }

        public void QueryAsync(string q, List<string> args, Action<List<List<string>>> action = null, bool non_query = false)
        {
            Common.QueryAsync(dbConn, Common._PrepareClear(_FixForSQLite(q), args, _PrepareArg), action, non_query, false);
            
        }
        private void SetTransState(bool state)            
        {
            if(state)
            {
                transaction = dbConn.BeginTransaction();
                //dbConn.BeginTransaction();
                trans_started = true;
            }
            else
            {
                if (commit_mode == CommitMode.NoCommit)
                    transaction.Rollback();
                else
                    transaction.Commit();
                //transaction.Dispose();
                trans_started = false;
            }
        }

        public DbConnection GetConn()
        { return dbConn; }

        public void Close()
        {
            dbConn.Close();
        }
        public bool Init()
        {
            SQLitePCL.Batteries.Init();
            return Common.Init(dbConn, "SQLite");
        }


    }
}
