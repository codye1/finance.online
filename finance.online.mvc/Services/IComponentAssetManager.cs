namespace finance.online.mvc.Services
{
    public interface IComponentAssetManager
    {
        void RegisterStyle(string src);
        void RegisterScript(string src, bool isModule = false);
        List<string> GetStyles();
        List<ScriptModel> GetScripts();
    }

    public class ScriptModel
    {
        public string Src { get; set; } = string.Empty;
        public bool IsModule { get; set; }
    }
}
