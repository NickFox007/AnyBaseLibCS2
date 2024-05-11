using AnyBaseLib.Bases;
using System.Text.RegularExpressions;

namespace AnyBaseLib;

public interface IAnyBase
{
    public void Set(CommitMode commit_mode, string db_name, string db_host = "", string db_user = "", string db_pass = "");
    public List<List<string>> Query(string q, List<string> args = null, bool non_query = false);
    public bool Init();
}