using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Transactions;
using MySqlConnector;

namespace AnyBaseLib.Bases
{
    internal class MySQLDriver : IAnyBase
    {
        private MySqlConnection dbConn;
        private CommitMode commit_mode;
        private bool trans_started;
        private DbTransaction transaction;
        private string builder;


        public void Set(CommitMode commit_mode, string db_name, string db_host, string db_user = "", string db_pass = "")
        {
            this.commit_mode = commit_mode;

            var db_host_arr = db_host.Split(":");
            var db_server = db_host_arr[0];
            uint db_port = 3306;
            if (db_host_arr.Length > 1)
                db_port = uint.Parse(db_host_arr[1]);


            builder = new MySqlConnectionStringBuilder
            {
                Server = db_server,
                Database = db_name,
                UserID = db_user,
                Password = db_pass,
                SslMode = MySqlSslMode.None,
                Port = db_port
                
            }.ConnectionString;

            dbConn = GetNewConn(false);

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
            }
        }

        public List<List<string>> Query(string q, List<string> args, bool non_query = false)
        {
            if (commit_mode == CommitMode.TimerCommit)
            {
                if (!trans_started && non_query) SetTransState(true);
                else
                {
                    if (trans_started && !non_query) SetTransState(false);
                }
            }

            return Common.Query(GetNewConn(), Common._PrepareClear(q, args), non_query);

        }

        public void QueryAsync(string q, List<string> args, Action<List<List<string>>> action = null, bool non_query = false)
        {

            var task = new Task<MySqlConnection>(() => GetNewConn());
            task.ContinueWith((conn) => Common.QueryAsync(conn.Result, Common._PrepareClear(q, args), action, non_query));            
            task.Start();
        }

        private MySqlConnection GetNewConn(bool open = true)
        {
            var conn = new MySqlConnection(builder);   
            //Console.WriteLine("[ !!! DEBUG !!! ] Get new conn...");
            if(conn != null && conn.State != ConnectionState.Open && open) conn.Open();
            dbConn = conn;
            return conn;
        }

        public DbConnection GetConn()
        { return dbConn; }

        public void Close()
        {
            if(dbConn != null && dbConn.State == ConnectionState.Open)
                dbConn.Close();
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
                //transaction.Dispose();
                trans_started = false;
            }
        }

        public bool Init()
        {
            return Common.Init(dbConn, "MySQL");
        }


    }
}
