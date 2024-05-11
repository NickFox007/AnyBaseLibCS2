using MySqlConnector;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace AnyBaseLib.Bases
{
    internal class PostgreDriver: IAnyBase
    {
        private NpgsqlConnection dbConn;
        private CommitMode commit_mode;
        private bool trans_started;
        private DbTransaction transaction;

        public void Set(CommitMode commit_mode, string db_name, string db_host, string db_user = "", string db_pass = "")
        {
            this.commit_mode = commit_mode;

            var builder = new NpgsqlConnectionStringBuilder
            {
                Host = db_host,
                Database = db_name,
                Username = db_user,
                Password = db_pass,
                SslMode = SslMode.Prefer,
                
            };


            dbConn = new NpgsqlConnection(builder.ConnectionString);
        }

        public List<List<string>> Query(string q, List<string> args, bool non_query = false)
        {

            if (commit_mode != CommitMode.AutoCommit)
            {
                if (!trans_started && non_query) SetTransState(true);
                else
                {
                    if (trans_started && !non_query) SetTransState(false);
                }
            }

            return Common.Query(dbConn, Common._PrepareClear(q, args), non_query);

        }

        private void SetTransState(bool state)
        {
            if (state)
            {
                transaction = dbConn.BeginTransaction();
                trans_started = true;
            }
            else
            {
                if (commit_mode == CommitMode.NoCommit)
                    transaction.Rollback();
                else
                    transaction.Commit();
                transaction.Dispose();
                trans_started = false;
            }
        }

        public bool Init()
        {
            return Common.Init(dbConn, "PostgreSQL");
        }
    }
}
