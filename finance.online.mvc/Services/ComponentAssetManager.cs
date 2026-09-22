namespace finance.online.mvc.Services
{
    public class ComponentAssetManager : IComponentAssetManager
    {
        private readonly List<string> _styles = new List<string>();
        private readonly List<ScriptModel> _scripts = new List<ScriptModel>();

        public void RegisterStyle(string src)
        {
            if (!_styles.Contains(src))
            {
                _styles.Add(src);
            }
        }

        public void RegisterScript(string src, bool isModule = false)
        {
            if (!_scripts.Any(s => s.Src == src))
            {
                _scripts.Add(new ScriptModel { Src = src, IsModule = isModule });
            }
        }

        public List<string> GetStyles()
        {
            return _styles;
        }

        public List<ScriptModel> GetScripts()
        {
            return _scripts;
        }
    }
}
