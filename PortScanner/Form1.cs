using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace PortScanner {
    public partial class Form1 : Form {
        public Form1() {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e) {
            numericUpDown2.Value = 1024;
            checkBox1.Checked = true;
            button4.Enabled = false;
            button6.Enabled = false;
        }

        //IP
        private IPAddress _ipAddress;

        private void textBox1_KeyDown(object sender, KeyEventArgs e) {
            if (e.KeyCode == Keys.Enter) {
                button1_Click(null, null);
            }
        }

        private void ipAddressControl1_Leave(object sender, EventArgs e) {
            if (ipAddressControl1.AnyBlank) {
                MessageBox.Show(@"IP input ERROR !");
            }
            else {
                _ipAddress = ipAddressControl1.IPAddress;
            }
        }

        private void button1_Click(object sender, EventArgs e) {
            if (textBox1.Text == "") {
                MessageBox.Show(@"Please input a URL or use localhost.");
            }

            try {
                IPHostEntry host = Dns.GetHostByName(textBox1.Text);
                _ipAddress = host.AddressList[0];
                ipAddressControl1.IPAddress = _ipAddress;
            }
            catch (Exception exception) {
                MessageBox.Show(exception.ToString());
            }
        }

        //Port
        private int _portBegin, _portEnd = 1024;

        private void checkBox3_CheckedChanged(object sender, EventArgs e) {
            numericUpDown2.Visible = !checkBox3.Checked;
            label3.Visible = !checkBox3.Checked;
            if (_portBegin >= _portEnd && !checkBox3.Checked) {
//                MessageBox.Show(@"Input Error!");
                numericUpDown1.Value = 0;
                numericUpDown2.Value = 1024;
            }
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e) {
            _portBegin = (int) numericUpDown1.Value;
            if (_portBegin >= _portEnd && !checkBox3.Checked) {
                MessageBox.Show(@"Input Error!");
            }
        }

        private void numericUpDown2_ValueChanged(object sender, EventArgs e) {
            _portEnd = (int) numericUpDown2.Value;
            if (_portBegin >= _portEnd) {
                MessageBox.Show(@"Input Error!");
            }
        }

        //Start
        private int _portAll;
        private bool _tcpScanner, _udpScanner;

        private void button3_Click(object sender, EventArgs e) {
            //check
            if (!checkBox1.Checked && !checkBox2.Checked) {
                MessageBox.Show(@"Must choose one from TCP and UDP !");
                return;
            }

            if (ipAddressControl1.AnyBlank) {
                MessageBox.Show(@"Must input right IP address !");
                return;
            }

            //clean dataGridView1
            dataGridView1.Rows.Clear();
            //bottom enable
            button4.Enabled = true;
            button3.Enabled = false;
            button6.Enabled = false;
            //time
            _watch.Restart();
            //start
            _tcpScanner = checkBox1.Checked;
            _udpScanner = checkBox2.Checked;
            _continue = 1;
            if (checkBox3.Checked) {
                _portEnd = _portBegin;
            }

            _portAll = _portEnd - _portBegin + 1;
            richTextBox1.AppendText("TCP/UDP port scanner start.\n");
            //ping
            Ping ping = new Ping();
            PingReply pingReply = ping.Send(_ipAddress);
            if (pingReply != null && pingReply.Status == IPStatus.Success) {
                richTextBox1.AppendText(_ipAddress + " ping time " + pingReply.RoundtripTime + "ms.\n");
            }
            else {
                richTextBox1.AppendText(_ipAddress + " unreachable.\n");
                //bottom enable
                button3.Enabled = true;
                button4.Enabled = false;
                return;
            }

            //bar
            progressBar1.Minimum = 0;
            progressBar1.Maximum = 0;
            if (_tcpScanner) {
                progressBar1.Maximum += _portAll;
            }

            if (_udpScanner) {
                progressBar1.Maximum += _portAll;
            }

            progressBar1.Value = 0;
            Application.DoEvents();

            //TCP/UDP port scanner (multi threading)
            ThreadPool.SetMaxThreads(1024, 1024);

            for (int iPort = _portBegin; iPort <= _portEnd; iPort++) {
                if (_tcpScanner) {
                    ThreadPool.QueueUserWorkItem(TCP_port_scanner, iPort);
                }

                if (_udpScanner) {
                    ThreadPool.QueueUserWorkItem(UDP_port_scanner, iPort);
                }
            }
        }

        //Stop
        private int _continue = 1;

        private void button4_Click(object sender, EventArgs e) {
            //stop
            _continue = 0;
            richTextBox1.AppendText("TCP/UDP port scanner stop.\n");
            //bottom enable
            button4.Enabled = false;
        }

        //About
        private void button2_Click(object sender, EventArgs e) {
            Form2 form2 = new Form2();
            form2.Show();
        }

        //Open previous result
        private void textBox4_Click(object sender, EventArgs e) {
            //Choose a file to open
            OpenFileDialog file = new OpenFileDialog {InitialDirectory = Application.StartupPath};
            file.ShowDialog();
            if (file.SafeFileName != "") {
                textBox4.Text = Path.GetFullPath(file.FileName);
            }
        }

        //open and show
        private void button5_Click(object sender, EventArgs e) {
            if (textBox4.Text == "") {
                MessageBox.Show(@"YOU HAVE NOT CHOOSE ONE FILE TO OPEN !");
                return;
            }

            if (!File.Exists(textBox4.Text)) {
                MessageBox.Show(@"CAN NOT OPEN THE FILE !");
                return;
            }

            //clean
            dataGridView1.Rows.Clear();
            //show
            StreamReader streamReader = new StreamReader(textBox4.Text);
            string readLine;
            while ((readLine = streamReader.ReadLine()) != null) {
                AddDataGridView(readLine);
            }
        }

        //Result
        delegate void AddDataGridViewDelegate(string str);

        public void AddDataGridView(string str) {
            if (dataGridView1.InvokeRequired) {
                AddDataGridViewDelegate d = AddDataGridView;
                dataGridView1.Invoke(d, str);
            }
            else {
                string[] strings = str.Split(' ');
                int index = dataGridView1.Rows.Add();
                for (int i = 0; i < 4; i++) {
                    dataGridView1.Rows[index].Cells[i].Value = strings[i];
                }

                if (checkBox4.Checked && !dataGridView1.Rows[index].Cells[3].Value.Equals("open")) {
                    dataGridView1.Rows[index].Visible = false;
                }
            }
        }

        //sort function
        private void dataGridView1_SortCompare(object sender, DataGridViewSortCompareEventArgs e) {
            if (e.Column == Column2) {
                e.SortResult = (Convert.ToInt32(e.CellValue1) - Convert.ToInt32(e.CellValue2) > 0) ? 1 :
                    (Convert.ToInt32(e.CellValue1) - Convert.ToInt32(e.CellValue2) < 0) ? -1 : 0;
            }
            else {
                e.SortResult = String.CompareOrdinal(Convert.ToString(e.CellValue1), Convert.ToString(e.CellValue2));
            }

            if (e.SortResult == 0 && e.Column != Column2) {
                e.SortResult =
                    (Convert.ToInt32(dataGridView1.Rows[e.RowIndex1].Cells[1].Value) -
                     Convert.ToInt32(dataGridView1.Rows[e.RowIndex2].Cells[1].Value) > 0) ? 1 :
                    (Convert.ToInt32(dataGridView1.Rows[e.RowIndex1].Cells[1].Value) -
                     Convert.ToInt32(dataGridView1.Rows[e.RowIndex2].Cells[1].Value) < 0) ? -1 : 0;
            }

            e.Handled = true;
        }

        //show open port only
        private void checkBox4_CheckedChanged(object sender, EventArgs e) {
            if (checkBox4.Checked) {
                for (int index = dataGridView1.Rows.Count - 1; index >= 0; index--) {
                    if (!dataGridView1.Rows[index].Cells[3].Value.Equals("open")) {
                        dataGridView1.Rows[index].Visible = false;
                    }
                }
            }
            else {
                for (int index = dataGridView1.Rows.Count - 1; index >= 0; index--) {
                    dataGridView1.Rows[index].Visible = true;
                }
            }
        }

        //save
        private void button6_Click(object sender, EventArgs e) {
            if (dataGridView1.Rows.Count == 0) {
                MessageBox.Show(@"NO DATA CAN BE SAVE !");
                return;
            }

            //create file
            string pathAndFile = Application.StartupPath + "\\" + _ipAddress + '_' +
                                 DateTime.Now.ToString("yyyy-MM-dd_hh-mm-ss") + ".log";
            StreamWriter streamWriter = new StreamWriter(pathAndFile, false);
            for (int index = 0; index < dataGridView1.Rows.Count; index++) {
                for (int i = 0; i < 4; i++) {
                    streamWriter.Write(dataGridView1.Rows[index].Cells[i].Value + " ");
                }

                streamWriter.WriteLine();
            }

            streamWriter.Close();
        }

        //Message
        public delegate void AddRichTextBox1Delegate(string str);

        public void AddRichTextBox1(string str) {
            if (richTextBox1.InvokeRequired) {
                AddRichTextBox1Delegate d = AddRichTextBox1;
                richTextBox1.Invoke(d, str);
            }
            else {
                richTextBox1.AppendText(str);
            }
        }

        //bar
        public delegate void AddBarDelegate(string str);

        private static readonly object ObjLock = new object();

        public void AddBar(string str) {
            if (progressBar1.InvokeRequired) {
                AddBarDelegate d = AddBar;
                progressBar1.Invoke(d, str);
            }
            else {
                lock (ObjLock) {
                    progressBar1.Value++;
                    if (progressBar1.Value == progressBar1.Maximum) {
                        if (_continue == 1) {
                            richTextBox1.AppendText("TCP/UDP port scanner finish.\n");
                        }

                        //bottom enable
                        button3.Enabled = true;
                        button4.Enabled = false;
                        button6.Enabled = true;
                        //time
                        _watch.Stop();
                    }

                    Application.DoEvents();
                    //time
                    label5.Text = (_watch.ElapsedMilliseconds / 1000.0) + @"s";
                }
            }
        }

        //time
        private readonly Stopwatch _watch = new Stopwatch();

        //TCP port scanner
        public void TCP_port_scanner(object iPort) {
            if (_continue == 0) {
                AddBar("");
                return;
            }

            int port = Convert.ToInt32(iPort);
            AddRichTextBox1(_ipAddress + " : " + port + " TCP scanning...\n");
            IPEndPoint ipEndPoint = new IPEndPoint(_ipAddress, port);
            try {
                TcpClient tcpClient = new TcpClient();
                tcpClient.ReceiveTimeout = tcpClient.SendTimeout = 1000;
                tcpClient.Connect(ipEndPoint);
                AddDataGridView(_ipAddress + " " + port + " TCP" + " open");
                tcpClient.Close();
            }
            catch {
                AddDataGridView(_ipAddress + " " + port + " TCP" + " close");
            }

            AddBar("");
        }

        //UDP port scanner
        public void UDP_port_scanner(object iPort) {
            if (_continue == 0) {
                AddBar("");
                return;
            }

            int port = Convert.ToInt32(iPort);
            AddRichTextBox1(_ipAddress + " : " + port + " UDP scanning...\n");
            IPEndPoint ipEndPoint = new IPEndPoint(_ipAddress, port);
            try {
                UdpClient udpClient = new UdpClient();
                udpClient.Connect(ipEndPoint);
                AddDataGridView(_ipAddress + " " + port + " UDP" + " open");
                udpClient.Close();
            }
            catch {
                AddDataGridView(_ipAddress + " " + port + " UDP" + " close");
            }

            AddBar("");
        }
    }
}