using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO.Compression;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Management.Instrumentation;

namespace YTMusic
{
    public partial class splash : Form
    {
        public splash()
        {
            InitializeComponent();
        }


        private void DownloadFile(String url, String dest)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage response = client.GetAsync(url).Result;
                    if (response.IsSuccessStatusCode)
                    {
                        Stream contentStream = response.Content.ReadAsStreamAsync().Result;
                        using (FileStream fileStream = File.Create(dest))
                        {
                            contentStream.CopyTo(fileStream);
                        }
                    }
                    else
                    {
                        MessageBox.Show($"Erreur de téléchargement des dépendances. Code: {response.StatusCode}");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Une erreur est suvernue lors du téléchargement des dépendances: {ex.Message}");
            }
        }

        static void ExtractExeFilesFromZip(string zipFilePath, string extractPath)
        {
            using (ZipArchive archive = ZipFile.OpenRead(zipFilePath))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    if (!string.IsNullOrEmpty(Path.GetExtension(entry.FullName)) &&
                    Path.GetExtension(entry.FullName).Equals(".exe", StringComparison.OrdinalIgnoreCase))
                    {
                        string destinationFileName = Path.GetFileName(entry.FullName);
                        string destinationPath = Path.Combine(extractPath, destinationFileName);

                        // Extract the .exe file
                        entry.ExtractToFile(destinationPath, true);
                    }
                }
            }
        }


        private void SetProgressValue(int val, string status)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<int, string>(SetProgressValue), val, status);
            }
            else
            {
                loadBar.Value = val;
                lblStatus.Text = status;
            }
        }


        private void StartMainForm()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(StartMainForm));
            }
            else
            {
                this.Hide();
                var main = new frmMain();
                main.Show();
            }
        }


        private void splash_Load(object sender, EventArgs e)
        {
            Task task = Task.Run(() => {
                // Download deps
                try
                {
                    Config cfg = Config.get();
                    TimeSpan difference = DateTime.Now - cfg.LastDownload;
                    bool should_redownload = difference.TotalDays > 2;
                    if (should_redownload || !File.Exists("yt-dlp.exe"))
                    {
                        SetProgressValue(5, "Téléchargement du programme Youtube...");
                        DownloadFile("https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe", "yt-dlp.exe");
                    }
                    SetProgressValue(40, "Téléchargement des codecs audio...");
                    if (should_redownload || !File.Exists("ffmpeg.exe"))
                    {
                        SetProgressValue(45, "Téléchargement des codecs audio...");
                        DownloadFile("https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip", "ffmpeg.zip");
                        SetProgressValue(75, "Extraction des codecs audio...");
                        ExtractExeFilesFromZip("ffmpeg.zip", Directory.GetCurrentDirectory());
                        File.Delete("ffmpeg.zip");
                        SetProgressValue(100, "Sauvegarde de la configuration...");
                    }
                    if (should_redownload) {
                        cfg.LastDownload = DateTime.Now;
                        cfg.save();
                    }
                    StartMainForm();
                }
                catch(Exception err) {
                    MessageBox.Show(err.Message);
                    Application.Exit();
                }
            });
        }
    }
}
