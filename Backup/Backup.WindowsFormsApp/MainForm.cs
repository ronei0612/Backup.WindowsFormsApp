using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Backup {
    public partial class MainForm : Form {
        public MainForm() {
            InitializeComponent();
        }

        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        const int SW_HIDE = 0;
        const int SW_SHOW = 5;

        public static string config = "backup-config.conf";
        public static string log, hour;
        public static List<string> comboboxList = new List<string>(),
            emailList = new List<string>(),
            sourceList = new List<string>(),
            destList = new List<string>();
        static string folder;
        Control o;
        readonly ToolTip toolTip = new ToolTip();
        readonly List<string> final = new List<string>(),
            _email = new List<string>();
        string[] _config;
        readonly Entry entry = new Entry();
        bool quit = false, running = false;
        ThreadStart ts;
        Thread backgroundThread;


        /// <summary>
        /// Load the config file.
        /// </summary>
        void ToLoad() {
            if (comboboxList.Count == 0) {
                comboboxList.Clear();
                foreach (string line in _config) {
                    if (line.Contains("=")) {
                        if (line.Split('=')[0] == "[email]")
                            foreach (string i in line.Split('=')[1].Split(';')) {
                                if (!string.IsNullOrEmpty(i))
                                    emailList.Add(i);
                            }
                        else if (line.Split('=')[0] == "[hora]")
                            hour = line.Split('=')[1];
                        else {
                            comboboxList.Add(line.Split('=')[0]);
                            sourceList.Add(line.Split('=')[1].Split(',')[0]);
                            destList.Add(line.Split('=')[1].Split(',')[1]);
                        }
                    }
                }
                ChangeElements(true);
            } else
                comboBox1.Items.Clear();
            foreach (string linha in comboboxList)
                comboBox1.Items.Add(linha);
            comboBox1.SelectedIndex = 0;

            ListToDataGrid(dataGridTo, emailList);
            if (hour != null)
            dateTimePicker1.Value = DateTime.Parse(hour);
        }

        void ArrayToDataGrid(DataGridView dataGrid, string[] array) {
            foreach (string item in array)
                dataGrid.Rows.Add(item);
        }

        void ListToDataGrid(DataGridView dataGrid, List<string> list) {
            foreach (string item in list)
                dataGrid.Rows.Add(item);
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e) {
            ChangeElements(true);
            int index = comboBox1.SelectedIndex;
            try {
                txtLog.Text = comboboxList[index];
                txtDest.Text = destList[index];
                txtSource.Text = sourceList[index];
            } catch { }
        }

        private void btnOk_Click(object sender, EventArgs e) {
            if (!running) {
                if (!Directory.Exists(txtSource.Text))
                    MessageBox.Show("Caminho não encontrado:" + Environment.NewLine + txtSource.Text);
                else
                    if (!Directory.Exists(txtDest.Text))
                        MessageBox.Show("Caminho não encontrado:" + Environment.NewLine + txtDest.Text);
                    else {
                        btnSave.PerformClick();
                        ChangeElements(false);
                        dataGridTo.Enabled = false;
                        this.Enabled = true;
                        btnOk.Image = WindowsFormsApp.Properties.Resources.gear;
                        ToRun();
                    }
            } else {
                ChangeElements(true);
                dataGridTo.Enabled = true;
                backgroundThread.Abort();
                running = false;
                btnOk.Text = "Executar";
                btnOk.Image = WindowsFormsApp.Properties.Resources.gear01;
            }
        }

        private void btnAdd_Click(object sender, EventArgs e) {
            ChangeElements(false);
            if (entry.ShowDialog() == DialogResult.OK) {
                comboboxList.Add(Entry.entry);
                ToLoad();
            }
            ChangeElements(true);
        }

        private void btnEdit_Click(object sender, EventArgs e) {
            int select = comboBox1.SelectedIndex;
            try {
                Entry.entry = comboBox1.SelectedItem.ToString();
                ChangeElements(false);
                if (entry.ShowDialog() == DialogResult.OK) {
                    comboboxList[select] = Entry.entry;
                    ToLoad();
                }
                ChangeElements(true);
            } catch { }
        }

        private void btnDel_Click(object sender, EventArgs e) {
            ChangeElements(false);
            int select = comboBox1.SelectedIndex;
            var messageYesNo = MessageBox.Show("Tem certeza que deseja deletar este item:" + Environment.NewLine + Environment.NewLine
                + comboBox1.SelectedItem.ToString(), "Atenção", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (messageYesNo == DialogResult.Yes) {
                comboboxList.RemoveAt(select);
                comboBox1.Items.Clear();
                ToLoad();
            }
            ChangeElements(true);
        }

        private void btnSave_Click(object sender, EventArgs e) {
            final.Clear();
            _email.Clear();
            if (ValidateElements()) {
                List<string> final = new List<string>();
                final.Add("[hora]=");
                final.Add(hour);
                final.Add(Environment.NewLine);

                int n = 0;
                foreach (string i in comboboxList) {
                    final.Add(i);
                    final.Add("=");
                    final.Add(sourceList[n]);
                    final.Add(",");
                    final.Add(destList[n]);
                    final.Add(Environment.NewLine);
                    n++;
                }
                final.Add(Environment.NewLine);
                File.WriteAllText(config, string.Join("", final.ToArray()).TrimEnd('\r', '\n'));
                File.AppendAllText(config, Environment.NewLine + "[email]=");
                string grid = DataGridToString(dataGridTo).Replace(Environment.NewLine, ";");
                try {
                    if (grid[grid.Length - 1] == ';')
                        grid = grid.Remove(grid.Length - 1);
                } catch { }
                File.AppendAllText(config, grid);
                lbSalvo.Text = "Item salvo!";
                lbSalvo.Visible = true;
            } else
                MessageBox.Show("Algo deu errado, favor verifique os dados, está faltando algo.");
        }


        bool ValidateElements() {
            int index = comboBox1.SelectedIndex;
            // If item in combobox
            try {
                if (txtDest.Text != "" && txtSource.Text != "" && dataGridTo.Rows[0].Cells[0].ToString() != "" && txtLog.Text != "") {
                    destList[index] = txtDest.Text;
                    sourceList[index] = txtSource.Text;
                    hour = dateTimePicker1.Value.ToString("HH:mm:ss");
                    emailList.Clear();
                    try {
                        string grid = DataGridToString(dataGridTo).Replace(Environment.NewLine, ";");
                        foreach (string i in grid.Replace(',', ';').Split(';'))
                            emailList.Add(i);
                    } catch {
                        emailList.Add(txtEmail.Text);
                    }
                    return true;
                } else
                    return false;
            // Combobox no item
            } catch {
                if (txtDest.Text != "" && txtSource.Text != "" && dataGridTo.Rows[0].Cells[0].ToString() != "" && txtLog.Text != "") {
                    destList.Add(txtDest.Text);
                    sourceList.Add(txtSource.Text);
                    hour = dateTimePicker1.Value.ToString("HH:mm:ss");
                    emailList.Clear();
                    foreach (string i in txtEmail.Text.Replace(',', ';').Split(';'))
                        emailList.Add(i);
                    return true;
                } else
                    return false;
            }
        }

        #region :::::::::::::::::::::::::::::::: Enable or disable buttons ::::::::::::::::::::::::::::::::::::::

        void ChangeElements(bool toEnable) {
            lbSalvo.Visible = false;
            lbSalvo.Text = "Item salvo!";
            if (toEnable)
                EnableControls(this);
            else
                DisableControls(this);
        }

        private void EnableControls(Control con) {
            foreach (Control c in con.Controls)
                EnableControls(c);
            con.Enabled = true;
        }

        private void DisableControls(Control con) {
            foreach (Control c in con.Controls)
                DisableControls(c);
            con.Enabled = false;
        }

        #endregion

        /// <summary>
        /// Open Directory from config
        /// </summary>
        string OpenDirectory(string local) {
            lbSalvo.Visible = false;
            lbSalvo.Text = "Item salvo!";
            OpenFileDialog folderBrowser = new OpenFileDialog {
                ValidateNames = false,
                CheckFileExists = false,
                CheckPathExists = true,
                InitialDirectory = local,
                FileName = "Selecione a pasta."
            };
            if (folderBrowser.ShowDialog() == DialogResult.OK)
                if (folderBrowser.FileName == "Selecione a pasta.")
                    return Path.GetDirectoryName(folderBrowser.FileName);
                else
                    return Path.GetDirectoryName(folderBrowser.FileName);
            else
                return "";
        }

        /// <summary>
        /// Open File from config
        /// </summary>
        string OpenFile(string local, string filter) {
            lbSalvo.Visible = false;
            lbSalvo.Text = "Item salvo!";
            OpenFileDialog fileBrowser = new OpenFileDialog {
                ValidateNames = false,
                CheckFileExists = false,
                CheckPathExists = true,
                InitialDirectory = local,
                Filter = filter
            };
            if (fileBrowser.ShowDialog() == DialogResult.OK)
                return fileBrowser.FileName;
            else
                return "";
        }

        /// <summary>
        /// Create a thread to Execute the task
        /// </summary>
        void ToRun() {
            if (!running) {
                btnOk.Text = "Parar";
                running = true;
                btnOk.Enabled = true;
                lbSalvo.Visible = false;
                btnConfigurar.Visible = false;
                backgroundThread = new Thread(ts);
                backgroundThread.Start();
                lbSalvo.Text = "Fim";
                lbSalvo.Visible = true;
            } else {
                btnOk.Text = "Executar";
                backgroundThread.Abort();
            }
        }

        /// <summary>
        /// Execute the task
        /// </summary>
        void Run() {
            while (true) {
                Wait(hour);
                // Show the Console
                var handle = GetConsoleWindow();
                ShowWindow(handle, SW_SHOW);                
                int n = 0;
                Log.EmailTo = emailList[0];
                if (emailList.Count > 1) {
                    emailList.RemoveAt(0);
                    Log.Cc = emailList.ToArray();
                }
                foreach (string i in comboboxList) {
                    Compare.Folders(sourceList[n], destList[n]);
                    Copy.ToCopy(sourceList[n], destList[n]);
                    File.AppendAllText(Log.AttachmentFile, Log._attachment);
                    File.AppendAllText(Log.saveLog, Log._attachment);
                    Log._attachment = "";
                    n++;
                }
                Copy.SendEmail();
                // Hide the Console
                handle = GetConsoleWindow();
                ShowWindow(handle, SW_HIDE);
            }
        }

        /// <summary>
        /// Wait until the scheduled task
        /// </summary>
        void Wait(string _hour) {
            int hour = Convert.ToInt32(_hour.Split(':')[0]);
            int min = Convert.ToInt32(_hour.Split(':')[1]);
            int sec = Convert.ToInt32(_hour.Split(':')[2]);
            while (true) {
                while (DateTime.Now.Hour != hour)
                    Thread.Sleep(1000);
                while (DateTime.Now.Minute != min)
                    Thread.Sleep(1000);
                while (DateTime.Now.Second != sec)
                    Thread.Sleep(100);
                break;
            }
        }

        /// <summary>
        /// Get cell value from datagridview and send to string variable
        /// </summary>
        string DataGridToString(DataGridView dataGrid) {
            StringBuilder sb = new StringBuilder();
            using (StringWriter tw = new StringWriter(sb)) {
                for (int i = 0; i < dataGrid.Rows.Count - 1; i++) {
                    for (int j = 0; j < dataGrid.Columns.Count; j++) {
                        try {
                            tw.Write($"{dataGrid.Rows[i].Cells[j].Value.ToString()}");
                            if (j != dataGrid.Columns.Count - 1)
                                tw.Write(";");
                        } catch { }
                    }
                    tw.WriteLine();
                }
                return sb.ToString();
            }
        }

        /// <summary>
        /// First verify the config files.
        /// </summary>
        private void VerifyFilesConfig() {
            Log.saveLog = Log.saveLog + "backup " + DateTime.Now.ToString("dd-MM-yyyy_HH-mm-ss") + ".log";
            File.WriteAllText(Log.saveLog, "");
            ts = new ThreadStart(Run);
            if (File.Exists(config))
                if (File.ReadAllText(config) == "")
                    File.Delete(config);
            if (!File.Exists(config)) {
                File.WriteAllText(config, "");
                btnAdd.Enabled = true;
            } else {
                _config = File.ReadAllLines(config);
                ToLoad();
            }
            // Hide the Console
            var handle = GetConsoleWindow();
            ShowWindow(handle, SW_HIDE);
            btnOk.PerformClick();
        }

        /// <summary>
        /// Then press Stop Button
        /// </summary>
        private void Stop() {
            if (pararToolStripMenuItem.Text == "Parar") {
                ChangeElements(true);
                running = false;
                pararToolStripMenuItem.Text = "Iniciar";
                btnConfigurar.Visible = true;
                btnOk.Text = "Executar";
            } else {
                running = true;
                ChangeElements(false);
                ToRun();
                pararToolStripMenuItem.Text = "Parar";
            }
        }

        private void btnSource_Click(object sender, EventArgs e) {
            folder = OpenDirectory(txtSource.Text);
            if (folder != "")
                txtSource.Text = folder;
        }

        private void btnDest_Click(object sender, EventArgs e) {
            folder = OpenDirectory(txtDest.Text);
            if (folder != "")
                txtDest.Text = folder;
        }

        private void btnLog_Click(object sender, EventArgs e) {
            folder = OpenDirectory(txtLog.Text);
            if (folder != "")
                txtLog.Text = folder;
        }

        private void txtEmail_TextChanged(object sender, EventArgs e) {
            lbSalvo.Visible = false;
            lbSalvo.Text = "Item salvo!";
        }

        private void txtLog_TextChanged(object sender, EventArgs e) {
            lbSalvo.Visible = false;
            lbSalvo.Text = "Item salvo!";
        }

        private void txtDest_MouseHover(object sender, EventArgs e) {
            o = (Control)sender;
            toolTip.Show(txtDest.Text, o, 10000);
        }

        private void txtSource_MouseHover(object sender, EventArgs e) {
            o = (Control)sender;
            toolTip.Show(txtSource.Text, o, 10000);
        }

        private void txtSource_MouseLeave(object sender, EventArgs e) {
            o = (Control)sender;
            toolTip.Hide(o);
        }

        private void txtDest_MouseLeave(object sender, EventArgs e) {
            o = (Control)sender;
            toolTip.Hide(o);
        }

        private void txtSource_KeyPress(object sender, KeyPressEventArgs e) {
            e.Handled = true;
        }

        private void txtDest_KeyPress(object sender, KeyPressEventArgs e) {
            e.Handled = true;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e) {
            if (!quit) {
                e.Cancel = true;
                Hide();
            } else
                if (backgroundThread != null)
                    try {
                        backgroundThread.Abort();
                    } catch { }
        }

        private void btnSair_Click(object sender, EventArgs e) {
            quit = true;
            Application.Exit();
        }

        private void pararToolStripMenuItem_Click(object sender, EventArgs e) {
            Stop();
        }

        private void btnIgnore_Click(object sender, EventArgs e) {
            folder = OpenFile(txtIgnore.Text, "Arquivo de texto (*.txt)|*.txt");
            if (folder != "")
                txtIgnore.Text = folder;
        }

        private void btnExit_Click(object sender, EventArgs e) {
            quit = true;
            Application.Exit();
        }

        private void btnConfigurar_Click(object sender, EventArgs e) {
            Show();
        }

        private void txtLog_KeyPress(object sender, KeyPressEventArgs e) {
            e.Handled = true;
        }

        private void MainForm_Load_1(object sender, EventArgs e) {
            VerifyFilesConfig();
        }

        private void notifyIcon1_Click(object sender, EventArgs e) {
            Show();
        }

    }
}
