using System;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Windows.Forms;

namespace YTMusic
{
    public partial class frmMain : Form
    {
        public frmMain()
        {
            InitializeComponent();
        }

        Process YTDLProcess = null;

        private bool isURLValid(string url)
        {
            if (!url.StartsWith("https://")) return false;
            if (!url.ToLower().Contains("youtube.com")) return false;
            return true;
        }


        private void AppendTextToEventLogs(string text)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(AppendTextToEventLogs), text);
            }
            else
            {
                txtEvents.AppendText(text);
            }
        }

        private void KillProcessAndChildren(int pid)
        {
            // Get all processes by parent ID
            ManagementObjectSearcher searcher = new ManagementObjectSearcher($"Select * From Win32_Process Where ParentProcessID={pid}");
            ManagementObjectCollection moc = searcher.Get();

            // Recursively kill each child process
            foreach (ManagementObject mo in moc)
            {
                int childPid = Convert.ToInt32(mo["ProcessID"]);
                KillProcessAndChildren(childPid);
            }

            // Kill the current process
            try
            {
                Process proc = Process.GetProcessById(pid);
                proc.Kill();
            }
            catch (ArgumentException)
            {
                // Process already exited
            }
        }

        private void btnDownload_Click(object sender, EventArgs e)
        {
            txtEvents.Clear();
            string url = txtURL.Text.Trim();
            if (!isURLValid(url))
            {
                txtEvents.AppendText("Lien invalide");
                return;
            }
            try
            {
                btnDownload.Enabled = false;
                txtURL.Enabled = false;

                // Check if we want to download the full playlist or just the current video
                bool downloadFullPlaylist = false;
                if (url.ToLower().Contains("list="))
                {
                    DialogResult result = MessageBox.Show(
                        "Voulez-vous télécharger la playlist au complet?",
                        "Confirmation téléchargement playlist",
                        MessageBoxButtons.YesNoCancel,
                        MessageBoxIcon.Question
                    );
                    if (result == DialogResult.Yes)
                    {
                        downloadFullPlaylist = true;
                    }
                    else if (result == DialogResult.Cancel)
                    {
                        btnDownload.Enabled = true;
                        txtURL.Enabled = true;
                        return;
                    }
                }

                // STart ytdl with our arguments
                String no_playlist_arg = downloadFullPlaylist ? "" : "--no-playlist";
                String dir = txtOutputDir.Text.Trim();               
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "yt-dlp.exe",
                    Arguments = $"--extract-audio --audio-format mp3 --no-playlist -o \"{dir}\\%(title)s.mp3\" \"{url}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                YTDLProcess = new Process
                {
                    StartInfo = startInfo,
                    EnableRaisingEvents = true
                };
                // Capture output to show user in our events box
                YTDLProcess.OutputDataReceived += (object snd, DataReceivedEventArgs ev) =>
                {
                    if (ev.Data != null)
                    {
                        try
                        {
                            Invoke(new Action<string>(AppendTextToEventLogs), ev.Data + Environment.NewLine);
                        }
                        catch { }
                    }
                };
                YTDLProcess.ErrorDataReceived += (snd, ev) =>
                {
                    if (ev.Data != null)
                    {
                        try
                        {
                            Invoke(new Action<string>(AppendTextToEventLogs), ev.Data + Environment.NewLine);
                        }
                        catch { }
                    }
                };
                // Reenable button when ytdlp exits
                YTDLProcess.Exited += (object snd, EventArgs ev) =>
                {
                    try { Invoke(new Action<string>(AppendTextToEventLogs), "Processus terminé!\n"); }
                    catch { }

                    try
                    {
                        Invoke(new Action(() =>
                        {
                            btnDownload.Enabled = true;
                            txtURL.Enabled = true;
                            MessageBox.Show("Fin du téléchargement");
                        }));
                    }
                    catch { }
                };

                // Actually start yt-dlp
                YTDLProcess.Start();
                YTDLProcess.BeginOutputReadLine();
                YTDLProcess.BeginErrorReadLine();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void btnSTOP_Click(object sender, EventArgs e)
        {
            // We forcefully kill yt-dlp and children
            if (YTDLProcess != null)
            {
                try
                {
                    KillProcessAndChildren(YTDLProcess.Id);
                }
                catch { }
                txtEvents.AppendText("Arrêt forcé du téléchargement\n");
            }
        }

        private void btnOutput_Click(object sender, EventArgs e)
        {
            // Select new output directory
            FolderBrowserDialog dirSelector= new FolderBrowserDialog();
            dirSelector.SelectedPath = txtOutputDir.Text;
            DialogResult result = dirSelector.ShowDialog();
            if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(dirSelector.SelectedPath))
            {
                string selectedFolder = dirSelector.SelectedPath;
                txtOutputDir.Text = selectedFolder;
                Config cfg = Config.get();
                cfg.DownloadDir = selectedFolder;
                cfg.save();
            }
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            // Get default dir
            string downloadsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            txtOutputDir.Text = downloadsPath;
            try
            {
                Config cfg = Config.get();
                if (Directory.Exists(cfg.DownloadDir))
                {
                    txtOutputDir.Text = cfg.DownloadDir;
                }
            }
            catch { }
        }

        private void frmMain_FormClosed(object sender, FormClosedEventArgs e)
        {
            Application.Exit();
        }
    }
}
