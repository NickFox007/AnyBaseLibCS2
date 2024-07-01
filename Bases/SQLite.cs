using Microsoft.Data.Sqlite;
//using System.Data.SQLite;

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlTypes;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
//using System.Data.SQLite;

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

            return Common.Query(dbConn, Common._PrepareClear(_FixForSQLite(q), args), non_query);
        }

        public void QueryAsync(string q, List<string> args, Action<List<List<string>>> action = null, bool non_query = false)
        {
            Common.QueryAsync(dbConn, Common._PrepareClear(_FixForSQLite(q), args), action, non_query, false);
        }
        /*
        public void QueryDapperAsync(Type type, string q, List<string> args = null, Action<object> action = null)
        {
            if (commit_mode != CommitMode.AutoCommit)
            {
                if (trans_started) SetTransState(false);
            }

            Common.QueryDapperAsync(dbConn, type, Common._PrepareClear(_FixForSQLite(q), args), action);
        }

        public object QueryDapper(Type type, string q, List<string> args = null, Action<object> action = null)
        {
            if (commit_mode != CommitMode.AutoCommit)
            {                
                if (trans_started) SetTransState(false);    
            }

            return Common.QueryDapper(dbConn, type, Common._PrepareClear(_FixForSQLite(q), args));
        }
        */
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

        public bool Init()
        {
            SQLitePCL.Batteries.Init();
            return Common.Init(dbConn, "SQLite");
        }


    }
}
