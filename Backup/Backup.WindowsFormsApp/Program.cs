using System;
using System.IO;
using System.Windows.Forms;

namespace Backup {
    static class Program {

        /// <summary>
        /// This is the Main.
        /// </summary>
        [STAThread]
        static void Main() {
            // Send the log file as an attachment
            Log.AttachmentFile = Path.Combine(Directory.GetCurrentDirectory(), "backup-log.txt");
            Log.EmailFrom = "FromMailAddress";
            Log.Pass = "MailPassword";

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}