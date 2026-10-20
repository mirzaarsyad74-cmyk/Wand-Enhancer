using System;
using System.IO;
using System.Reflection;

namespace WandEnhancer.Core
{
    public static class AvatarAsset
    {
        private static string _dataUri;
        private static readonly object _sync = new object();

        public static string DataUri
        {
            get
            {
                if (_dataUri != null) return _dataUri;
                lock (_sync)
                {
                    if (_dataUri != null) return _dataUri;
                    _dataUri = LoadDataUri();
                    return _dataUri;
                }
            }
        }

        private static string LoadDataUri()
        {
            try
            {
                using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("patches/avatar.png"))
                {
                    if (stream != null)
                    {
                        using (var ms = new MemoryStream())
                        {
                            stream.CopyTo(ms);
                            return "data:image/png;base64," + Convert.ToBase64String(ms.ToArray());
                        }
                    }
                }
            }
            catch
            {
            }
            return "";
        }
    }
}
