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
        private bool trans_started;
        private DbTransaction transaction;

        public void Set(CommitMode commit_mode, string db_name, string db_host = "", string db_user = "", string db_pass = "")
        {            
            this.commit_mode = commit_mode;
            dbConn = new SqliteConnection($"Data Source={db_name}.sqlite;");
        }

        private string _FixForSQLite(string q)
        {            
            var new_q = q;
            if (new_q.Contains("CREATE TABLE ") && new_q.Contains(" PRIMARY KEY")) new_q = $"{new_q} WITHOUT ROWID";

            return new_q;
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
                if(commit_mode == CommitMode.NoCommit)
                    transaction.Rollback();
                else
                    transaction.Commit();
                transaction.Dispose();
                trans_started = false;
            }
        }

        public bool Init()
        {
            SQLitePCL.Batteries.Init();
            return Common.Init(dbConn, "SQLite");
        }


    }
}
