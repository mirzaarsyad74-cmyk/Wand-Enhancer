using Newtonsoft.Json;

namespace WandEnhancer.Models
{
    public class WeModConfig
    {
        public string BrandName { get; set; }
        public string ExecutableName { get; set; }
        public string RootDirectory { get; set; }
        
        [JsonIgnore]
        public string ExecutablePath => System.IO.Path.Combine(RootDirectory, ExecutableName);

        [JsonIgnore]
        public string Version
        {
            get
            {
                try
                {
                    if (System.IO.File.Exists(ExecutablePath))
                    {
                        var info = System.Diagnostics.FileVersionInfo.GetVersionInfo(ExecutablePath);
                        var ver = info.ProductVersion ?? info.FileVersion;
                        if (!string.IsNullOrWhiteSpace(ver))
                        {
                            return ver.Trim();
                        }
                    }
                }
                catch
                {
                    // Ignore file version read errors
                }

                try
                {
                    var dirName = System.IO.Path.GetFileName(RootDirectory);
                    if (dirName != null && dirName.StartsWith("app-", System.StringComparison.OrdinalIgnoreCase))
                    {
                        return dirName.Substring(4);
                    }
                }
                catch
                {
                    // Ignore directory name parsing errors
                }

                return null;
            }
        }

        public override string ToString()
        {
            return RootDirectory;
        }
    }
}