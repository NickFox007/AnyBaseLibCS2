using AnyBaseLib.Bases;
using System.Data.Common;
namespace AnyBaseLib;

public interface IAnyBase
{
    public void Set(CommitMode commit_mode, string db_name, string db_host = "", string db_user = "", string db_pass = "");
    public List<List<string>> Query(string q, List<string> args = null, bool non_query = false);
    public void QueryAsync(string q, List<string> args = null, Action<List<List<string>>> action = null, bool non_query = false);
    public DbConnection GetConn();

    public bool Init();
    public void Close();
}