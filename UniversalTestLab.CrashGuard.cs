// UniversalTestLab.CrashGuard.cs
// Global crash reporting: unhandled exceptions (UI + non-UI + background tasks)
// are written to a timestamped crash log under %LOCALAPPDATA%\UniversalTestLab,
// and the user is shown a bilingual dialog with the log path plus Copy / Open /
// clipboard actions so any problem can be reported with a full stack trace.
// ============================================================================
using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace UniversalTestLab
{
    internal static class CrashGuard
    {
        private const string ProjectIssuesUrl = "https://github.com/VanillaWong/Universal-Test-Lab-Vanilla-Version/issues";
        private static readonly object Sync = new object();
        private static DateTime _lastDialogUtc = DateTime.MinValue;
        private static string _latestLogPath = "";

        internal static string LatestLogPath { get { return _latestLogPath; } }

        /// <summary>Registers every global exception sink. Call once at startup.</summary>
        internal static void Install()
        {
            try { Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException); } catch { }
            Application.ThreadException += delegate(object s, System.Threading.ThreadExceptionEventArgs e)
            {
                Handle(e.Exception, "UI thread");
            };
            AppDomain.CurrentDomain.UnhandledException += delegate(object s, UnhandledExceptionEventArgs e)
            {
                Exception ex = e.ExceptionObject as Exception ?? new Exception(Convert.ToString(e.ExceptionObject, CultureInfo.InvariantCulture));
                Handle(ex, e.IsTerminating ? "AppDomain (process terminating)" : "AppDomain");
            };
            try
            {
                TaskScheduler.UnobservedTaskException += delegate(object s, UnobservedTaskExceptionEventArgs e)
                {
                    try { WriteLog(e.Exception, "background task"); } catch { }
                    try { e.SetObserved(); } catch { }
                };
            }
            catch { }
        }

        private static void Handle(Exception ex, string source)
        {
            string path = "";
            try { path = WriteLog(ex, source); } catch { }
            try
            {
                lock (Sync)
                {
                    DateTime now = DateTime.UtcNow;
                    if ((now - _lastDialogUtc).TotalSeconds < 10) return;
                    _lastDialogUtc = now;
                }
                ShowDialog(ex, path, source);
            }
            catch { }
        }

        private static string BuildReport(Exception ex, string source)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Universal Test Lab crash report");
            sb.AppendLine("===============================");
            try
            {
                Assembly asm = Assembly.GetExecutingAssembly();
                sb.AppendLine("Version: " + (asm.GetName().Version == null ? "?" : asm.GetName().Version.ToString()));
                AssemblyInformationalVersionAttribute info =
                    Attribute.GetCustomAttribute(asm, typeof(AssemblyInformationalVersionAttribute)) as AssemblyInformationalVersionAttribute;
                sb.AppendLine("Informational: " + (info == null ? "?" : info.InformationalVersion));
            }
            catch { }
            sb.AppendLine("Generated: " + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) + " UTC");
            try { sb.AppendLine("OS: " + Environment.OSVersion); } catch { }
            try { sb.AppendLine("CLR: " + Environment.Version); } catch { }
            try { sb.AppendLine("Culture: " + CultureInfo.CurrentCulture.Name + " / UI: " + CultureInfo.CurrentUICulture.Name); } catch { }
            try { sb.AppendLine("Executable: " + Process.GetCurrentProcess().MainModule.FileName); } catch { }
            try { sb.AppendLine("Exe dir: " + AppDomain.CurrentDomain.BaseDirectory); } catch { }
            try { sb.AppendLine("LocalAppData: " + Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)); } catch { }
            sb.AppendLine("Source: " + source);
            sb.AppendLine();
            sb.AppendLine("Exception:");
            sb.AppendLine(ex == null ? "(null)" : ex.ToString());
            sb.AppendLine();
            sb.AppendLine("Report it: " + ProjectIssuesUrl);
            return sb.ToString();
        }

        private static string WriteLog(Exception ex, string source)
        {
            string text = BuildReport(ex, source);
            string stamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
            string[] roots =
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "UniversalTestLab"),
                AppDomain.CurrentDomain.BaseDirectory
            };
            string written = "";
            foreach (string root in roots)
            {
                try
                {
                    Directory.CreateDirectory(root);
                    string latest = Path.Combine(root, "crash.log");
                    File.WriteAllText(latest, text);
                    string snapshot = Path.Combine(root, "crash_" + stamp + ".log");
                    File.WriteAllText(snapshot, text);
                    if (String.IsNullOrEmpty(written)) written = latest;
                }
                catch { }
            }
            _latestLogPath = written;
            return written;
        }

        private static void ShowDialog(Exception ex, string logPath, string source)
        {
            Form form = new Form();
            form.Text = "Universal Test Lab — Unexpected error / 意外错误";
            form.StartPosition = FormStartPosition.CenterScreen;
            form.Width = 720;
            form.Height = 520;
            form.MinimumSize = new Size(560, 380);
            form.MaximizeBox = false;
            form.FormBorderStyle = FormBorderStyle.Sizable;

            string logLine = String.IsNullOrEmpty(logPath)
                ? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\UniversalTestLab\\crash.log"
                : logPath;

            TextBox body = new TextBox();
            body.Multiline = true;
            body.ReadOnly = true;
            body.ScrollBars = ScrollBars.Both;
            body.WordWrap = false;
            body.Font = new Font("Consolas", 9F);
            body.Dock = DockStyle.Fill;
            StringBuilder text = new StringBuilder();
            text.AppendLine("Something went wrong. A crash log has been saved, please send it to the developer");
            text.AppendLine("(GitHub Issues) together with a short description of what you were doing.");
            text.AppendLine();
            text.AppendLine("程序遇到了意外错误。崩溃日志已保存,请把它连同你的操作描述一起发给开发者(GitHub Issues),");
            text.AppendLine("以便定位并修复问题。");
            text.AppendLine();
            text.AppendLine("Log file / 日志文件: " + logLine);
            text.AppendLine("Source / 来源: " + source);
            text.AppendLine();
            text.AppendLine("Issues: " + ProjectIssuesUrl);
            text.AppendLine();
            text.AppendLine("------------------------------------------------------------");
            text.AppendLine();
            text.Append(ex == null ? "(null exception)" : ex.ToString());
            body.Text = text.ToString();

            Button copyButton = new Button();
            copyButton.Text = "Copy log / 复制日志";
            copyButton.Width = 140;
            copyButton.Click += delegate
            {
                try
                {
                    string full = File.Exists(logPath) ? File.ReadAllText(logPath) : body.Text;
                    Clipboard.SetText(full);
                }
                catch { }
            };

            Button openButton = new Button();
            openButton.Text = "Open folder / 打开日志文件夹";
            openButton.Width = 180;
            openButton.Click += delegate
            {
                try
                {
                    string dir = String.IsNullOrEmpty(logPath) ? Path.GetDirectoryName(logPath) : Path.GetDirectoryName(logPath);
                    if (String.IsNullOrEmpty(dir))
                        dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "UniversalTestLab");
                    Directory.CreateDirectory(dir);
                    Process.Start("explorer.exe", "\"" + dir + "\"");
                }
                catch { }
            };

            Button closeButton = new Button();
            closeButton.Text = "Close / 关闭";
            closeButton.Width = 100;
            closeButton.DialogResult = DialogResult.OK;

            Panel buttons = new Panel();
            buttons.Dock = DockStyle.Bottom;
            buttons.Height = 44;
            buttons.Controls.Add(copyButton);
            buttons.Controls.Add(openButton);
            buttons.Controls.Add(closeButton);
            buttons.Resize += delegate
            {
                int y = (buttons.Height - closeButton.Height) / 2;
                int x = buttons.ClientSize.Width - 16;
                closeButton.Location = new Point(x - closeButton.Width, y);
                x = closeButton.Left - 10;
                openButton.Location = new Point(x - openButton.Width, y);
                x = openButton.Left - 10;
                copyButton.Location = new Point(x - copyButton.Width, y);
            };

            form.Controls.Add(body);
            form.Controls.Add(buttons);
            form.Resize += delegate { body.Invalidate(); };
            try { form.ShowDialog(); } catch { }
            try { form.Dispose(); } catch { }
        }
    }
}
