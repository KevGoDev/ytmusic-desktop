using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YTMusic
{
    public class Config
    {
        private static string NOW_STR = DateTime.Now.ToString("yyyy-MM-dd");
        private static string DEFAULT_CFG = $"DIR=\nLAST_DOWNLOAD={NOW_STR}";
        private static string CFG_NAME = "config.cfg";

        public string DownloadDir;
        public DateTime LastDownload;

        public Config(string download_dir, DateTime last_download) {
            DownloadDir = download_dir;
            LastDownload = last_download;
        }

        public static Config get() {
            if (!File.Exists(CFG_NAME)) {
                File.WriteAllText(CFG_NAME, DEFAULT_CFG);
            }
            string[] content = File.ReadAllLines(CFG_NAME);
            Dictionary<string, string> cfg = new Dictionary<string, string>();
            foreach(string line in content)
            {
                string[] parts = line.Split('=');
                if(parts.Length == 2) {
                    cfg[parts[0].Trim().ToUpper()] = parts[1].Trim();
                }
            }

            string download_dir = cfg.ContainsKey("DIR") ? cfg["DIR"] : null;
            string last_download_str = cfg.ContainsKey("LAST_DOWNLOAD") ? cfg["LAST_DOWNLOAD"] : null;
            DateTime last_download = DateTime.Now;
            if (!DateTime.TryParse(last_download_str, out last_download))
            {
                DateTime.TryParse("1900-01-01", out last_download);
            }
            return new Config(download_dir, last_download);
        }

        public void save() {
            string dir = DownloadDir == null ? "" : DownloadDir;
            string ld = LastDownload.ToString("yyyy-MM-dd");
            string cfg = $"DIR={dir}\nLAST_DOWNLOAD={ld}";
            File.WriteAllText(CFG_NAME, cfg);
        }
    }
}
