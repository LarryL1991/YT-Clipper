using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace YT_Clipper
{
    public partial class Form1 : Form
    {
        private string DL_Vid_Name;
        public Form1()
        {
            InitializeComponent();
        }

        private void endBox_Leave(object sender, System.EventArgs e)
        {
            if (endBox.Text != "")
            {
                lengthBox.ReadOnly = true;
                lengthBox.BackColor = Color.LightGray;
                lengthBox.TabStop = false;
            }
            else
            {
                lengthBox.ReadOnly = false;
                lengthBox.BackColor = Color.White;
                lengthBox.TabStop = true;
            }
            
        }

        private void endBox_Enter(object sender, System.EventArgs e)
        {
            if (endBox.ReadOnly == true)
            {
                lengthBox.Focus();
            }
        }

        private void lengthBox_Leave(object sender, System.EventArgs e)
        {
            if (lengthBox.Text != "")
            {
                endBox.ReadOnly = true;
                endBox.BackColor = Color.LightGray;
                endBox.TabStop = false;
            }
            else
            {
                endBox.ReadOnly = false;
                endBox.BackColor = Color.White;
                endBox.TabStop = true;
            }
        }

        private void lengthBox_Enter(object sender, System.EventArgs e)
        {
            if(lengthBox.ReadOnly == true)
            {
                directoryBox.Focus();
            }
        }

        private int readTime(string timeString)
        {
            bool succeeded;
            int sIndex  = 0;
            int sIndex2 = 0;
            int seconds;
            int minutes;
            int hours;

            //string source = timeString;
            if (String.IsNullOrEmpty(timeString))
                return 0;
            
            //count how many colons are in the start/end box
            int colonCount = 0;
            foreach (char c in timeString)
                if (c == ':') colonCount++;

            switch(colonCount)
            {
                case 0:
                    succeeded = Int32.TryParse(timeString, out seconds);
                    if (succeeded)
                        return seconds; //no colons and time successfully parsed.
                    else
                        return -1;
                case 1:
                    sIndex = timeString.IndexOf(":", 0);
                    succeeded = Int32.TryParse(timeString.Substring(0, sIndex), out minutes);
                    if (succeeded)
                    {
                        succeeded = Int32.TryParse(timeString.Substring(sIndex + 1), out seconds);
                        if (succeeded)
                            return (minutes * 60) + seconds;

                    }
                    return -1;
                case 2:
                    sIndex = timeString.IndexOf(":", 0);
                    sIndex2 = timeString.IndexOf(":", sIndex + 1);
                    succeeded = Int32.TryParse(timeString.Substring(0, sIndex), out hours);
                    if(succeeded)
                    {
                       
                        succeeded = Int32.TryParse(timeString.Substring(sIndex + 1, timeString.Length - sIndex2 - 1), out minutes);
                        if(succeeded)
                        {
                            succeeded = Int32.TryParse(timeString.Substring(sIndex2 + 1), out seconds);
                            if (succeeded)
                                return (hours * 3600) + (minutes * 60) + seconds;
                        }
                    }
                    return -1;
                default:
                    return -1;
            };
        }

        private void appendTextBox(string message)
        {
            if (InvokeRequired)
            {
                this.BeginInvoke(new Action<string>(appendTextBox), new object[] { message });
                return;
            }

            if (string.IsNullOrEmpty(message))
                return;

            CMDBox.Text += message + "\n";
            CMDBox.SelectionStart = CMDBox.Text.Length;
            CMDBox.ScrollToCaret();
            if (message.Contains("[ffmpeg] Merging formats into ")) //Might be a bad way of doing this. :think:
            {
                int uIndex = message.LastIndexOf("\\");
                //CMDBox.Text += "\n" + message.Substring(uIndex + 1, message.Length - (uIndex + 2)) + "\n";
                DL_Vid_Name = message.Substring(uIndex + 1, message.Length - (uIndex + 2));
                CMDBox.Text += "\n" + DL_Vid_Name + "\n\n";
            }
        }

        private void changeStatusMessage(string message)
        {
            if (InvokeRequired)
            {
                this.BeginInvoke(new Action<string>(changeStatusMessage), new object[] { message });
                return;
            }
            statusLabel.Text = message;
        }

        void cmd_DataReceived(object sender, DataReceivedEventArgs e)
        {
            appendTextBox(e.Data);
        }

        void cmd_Error(object sender, DataReceivedEventArgs e)
        {
            //Console.WriteLine("Error from other process");
            //Console.WriteLine(e.Data);
            //CMDBox.Text += "ERROR: " + e.Data;
            //MessageBox.Show(e.Data);
            appendTextBox(e.Data);
        }

        void runCMD(string arg)
        {
            ProcessStartInfo cmdStartInfo = new ProcessStartInfo();
            cmdStartInfo.FileName = @"C:\Windows\System32\cmd.exe";
            cmdStartInfo.RedirectStandardOutput = true;
            cmdStartInfo.RedirectStandardError = true;
            cmdStartInfo.RedirectStandardInput = true;
            cmdStartInfo.UseShellExecute = false;
            cmdStartInfo.CreateNoWindow = true;

            Process cmdProcess = new Process();
            cmdProcess.StartInfo = cmdStartInfo;
            cmdProcess.ErrorDataReceived += cmd_Error;
            cmdProcess.OutputDataReceived += cmd_DataReceived;
            cmdProcess.EnableRaisingEvents = true;
            cmdProcess.Start();
            cmdProcess.BeginOutputReadLine();
            cmdProcess.BeginErrorReadLine();

            cmdProcess.StandardInput.WriteLine(arg);     //Execute ping bing.com
            //cmdProcess.StandardInput.WriteLine("exit");                  //Execute exit.
            cmdProcess.StandardInput.Flush();
            cmdProcess.StandardInput.Close();
            cmdProcess.WaitForExit();
        }

        void doAsyncExecute(int selection, string URL, int start, int length, string directory, string file_name)
        {
            changeStatusMessage("Downloading Youtube video");
            runCMD("youtube-dl.exe --no-color --no-playlist --newline -o \"%cd%\\videos\\%(title)s-%(id)s.%(ext)s\" " + URL);
            //if user wants it cut:
            changeStatusMessage("Cutting video to specifications");
            string commandBuilder = "ffmpeg.exe";

            if(start > 0)
                commandBuilder += " -ss " + start;
            if (length > 0)
                commandBuilder += " -t " + length;

            commandBuilder += " -i \"%cd%\\videos\\" + DL_Vid_Name + "\"";

            if(string.IsNullOrEmpty(directory))
                commandBuilder += " \"%cd%\\cut-videos\\";
            else
                commandBuilder += " \"" + directory;

            if(string.IsNullOrEmpty(file_name))
                commandBuilder += DL_Vid_Name + "\"";
            else 
                commandBuilder += file_name + DL_Vid_Name.Substring(DL_Vid_Name.Length - 4) + "\"";

            runCMD(commandBuilder);

            //runCMD("ffmpeg.exe -ss 10 -t 60 -i \"%cd%\\videos\\" + DL_Vid_Name + "\" \"%cd%\\videos\\out1.mp4\"");
            changeStatusMessage("Video cut to specifications");
        }

        private bool inRange(int number, int a, int b)
        {
            if (number >= a && number <= b)
                return true;
            else
                return false;
        }

        private string checkFileName(string sFile)
        {
            int charVal;
            if (sFile.Length > 150)
                sFile = sFile.Substring(0, 150);

            string sBuilder = "";
            for (int i = 0; i < sFile.Length; i++)
            {
                charVal = (int)sFile[i];
                if (inRange(charVal, 40, 41) || inRange(charVal, 43, 46) || inRange(charVal, 48, 57) || inRange(charVal, 65, 91)
                    || charVal == 93 || inRange(charVal, 95, 123) || charVal == 125)
                    sBuilder += sFile[i];
                else
                    sBuilder += "-";
            }
            return sBuilder;
        }

        private void goButton_Click(object sender, EventArgs e)
        {
            var URL = linkBox.Text;
            int start = readTime(startBox.Text);
            int end = readTime(endBox.Text);
            int length = readTime(lengthBox.Text);
            var directory = directoryBox.Text + "\\";
            //var file_name = nameBox.Text;
            //perform check to see if file name is valid
            string file_name = checkFileName(nameBox.Text);

            CMDBox.Text = "";

            if (start < 0 || end < 0 || length < 0)
            {
                MessageBox.Show("Start and End/Length need to be in seconds or HH:MM:SS");
                return;
            }

            if (start >= end && start != 0 && end != 0)
            {
                MessageBox.Show("End time cannot be less than start time!");
                return;
            }

            if (length == 0 && end > 0)
            {
                length = end - start;
            }

            //Perform check to see if directory is valid


            //perform check to see if overwriting an existing file (or add an overwrite? checkbox..)

            Thread thr = new Thread(()=>doAsyncExecute(1, URL, start, length, directory, file_name));
            thr.Start();

        }

        private void fileButton_Click(object sender, EventArgs e)
        {
            //https://stackoverflow.com/questions/11624298/how-to-use-openfiledialog-to-select-a-folder
            using (var fbd = new FolderBrowserDialog())
            {
                DialogResult result = fbd.ShowDialog();

                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                    directoryBox.Text = fbd.SelectedPath;
            }
        }
    }
}
