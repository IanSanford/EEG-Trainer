using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO.Ports;
using System.IO;
using System.Threading;

/*  
    EEG Trainer  -by Ian Sanford
    Connects and communicates with EEG-SMT headset 
    Records EEG data along with desired output (left, right, up, down, click)
    in a format compatable with a feed-forward neural network.

*/


namespace EEGTrainer
{
    public partial class Form1 : Form
    {
        SerialPort ComPort = new SerialPort();

        internal delegate void SerialDataReceivedEventHandlerDelegate(
                 object sender, SerialDataReceivedEventArgs e);
        
        internal delegate void SerialPinChangedEventHandlerDelegate(
                 object sender, SerialPinChangedEventArgs e);

        private SerialPinChangedEventHandler SerialPinChangedEventHandler1;
        delegate void SetTextCallback(string text);
        string InputData = String.Empty;

        public delegate void AddDataDelegate(String myString);
        public AddDataDelegate myDelegate;

        string DIR;
        volatile string toFile, outputs;
        float timeLeft;
        float timeBetween;
        bool timerGo;
        DateTime startTime;
        volatile bool stopOutThread;
        int count, packetCount;
        Thread outThread;
        bool record;
        bool connected;
        int trials;
        volatile bool reject;
        int rejectT;
        volatile int c1_errors, c2_errors;
        string input;
        volatile bool c1_good, c2_good;

        public Form1()
        {
            InitializeComponent();
            connected = false;
            this.myDelegate = new AddDataDelegate(AddDataMethod);
            toFile = "";
            rejectT = 3; //reject threshold. amount of OOB packets allowed in a single trial
            timeLeft = 60;
            timeBetween = 30;
            timerGo = false;
            DIR = @"c:\EEG Trial Data\";
            count = 0;
            packetCount = 0;
            record = false;
            outThread = new Thread(new ThreadStart(outputThread));
            stopOutThread = false;
        }

        public void outputThread()
        {

            //Console.WriteLine("out thread started");
            byte[] first = new byte[1];
            byte[] second = new byte[1];
            byte[] byteArray = new byte[15];
            ushort[] data = new ushort[6];

            while (!stopOutThread)
            {
                while ( (ComPort.BytesToRead > 17) )
                {
                    if (record && packetCount < 256)
                    {
                        ++count;
                        ++packetCount;
                    }

                    //matches first 17 bytes
                    bool match = false;
                    while (!match)
                    {
                        //matching first sync byte
                        ComPort.Read(first, 0, 1);
                        if (first[0] == 165)
                        {
                            //matching second sync byte
                            ComPort.Read(second, 0, 1);
                            if (second[0] == 90)
                            {
                                match = true;
                                ComPort.Read(byteArray, 0, 15);

                                //adds packet count to string, testing only
                                if (timerGo)
                                {
                                    //toFile += count;
                                    //toFile += " ";
                                }
                            }
                        }
                    }

                    //loops through channel data in packet
                    //EEG-SMT only uses channels 0 and 1
                    for (int it = 0; it < 6; ++it)
                    {
                        int foo = (2 * it + 2);
                        data[it] = BitConverter.ToUInt16(byteArray, foo);
                        byte[] b = BitConverter.GetBytes(data[it]);
                        Array.Reverse(b); //fix reverse byte order
                                          //adds first two channels data to file

                        //channel 1
                        if ((it == 0) && (timerGo))
                        {
                            double preScale = BitConverter.ToUInt16(b, 0);
                            double scaled = (preScale / 1023) * 2 - 1;
                            if(scaled == 1 || scaled == -1)
                            {
                                c1_good = false;
                                if (record)
                                {
                                    ++c1_errors;
                                }
                            }
                            else
                            {
                                c1_good = true;
                            }
                            //only add to file if recording
                            if (record && packetCount < 256)
                            {
                                toFile += scaled.ToString("N6");
                                toFile += ",";
                            }
                        }

                        //channel 2
                        if ((it == 1) && (timerGo))
                        {
                            double preScale = BitConverter.ToUInt16(b, 0);
                            double scaled = (preScale / 1023) * 2 - 1;
                            if (scaled == 1 || scaled == -1)
                            {
                                c2_good = false;
                                if (record)
                                {
                                    ++c2_errors;
                                }
                            }
                            else
                            {
                                c2_good = true;
                            }
                            //only add to file if recording
                            if (record && packetCount < 256)
                            {
                                toFile += scaled.ToString("N6");
                                if (packetCount != 256)
                                {
                                    toFile += ",";
                                }
                            }
                        }
                    }
                    //reject if too many OOB data points
                    if(c1_errors >= rejectT || c2_errors >= rejectT)
                    {
                        reject = true;
                    }
                    Thread.Sleep(0);
                }
            }
            //Console.WriteLine("left in buffer: " + ComPort.BytesToRead);
        }

        public void AddDataMethod(String myString) {}

        private void textBox1_TextChanged(object sender, EventArgs e) { }

        private void button1_Click(object sender, EventArgs e)
        {
            string[] ArrayComPortsNames = null;
            int index = -1;
            string ComPortName = null;

            ArrayComPortsNames = SerialPort.GetPortNames();
            do
            {
                index += 1;
                comboBox1.Items.Add(ArrayComPortsNames[index]);
            }
            while (!((ArrayComPortsNames[index] == ComPortName)
                          || (index == ArrayComPortsNames.GetUpperBound(0))));
            Array.Sort(ArrayComPortsNames);

            //want to get first out
            if (index == ArrayComPortsNames.GetUpperBound(0))
            {
                ComPortName = ArrayComPortsNames[0];
            }
            comboBox1.Text = ArrayComPortsNames[0];
            comboBox2.Items.Add(57600);
            comboBox2.Items.ToString();
            //get first item print in text
            comboBox2.Text = comboBox2.Items[0].ToString();
            comboBox3.Items.Add(8);
            comboBox3.Items.Add(7);
            comboBox3.Text = comboBox3.Items[0].ToString();
        }

        private void mySerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            Thread.Sleep(1);
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e) { }
        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e) { }
        private void comboBox3_SelectedIndexChanged(object sender, EventArgs e) { }

        private void button2_Click(object sender, EventArgs e)
        {
            if (button2.Text == "Connect")
            {
                //button2.Text = "Open";
                ComPort.PortName = Convert.ToString(comboBox1.Text);
                ComPort.BaudRate = Convert.ToInt32(comboBox2.Text);
                ComPort.DataBits = Convert.ToInt16(comboBox3.Text);
                ComPort.ReadBufferSize = 100000;
                ComPort.StopBits = (StopBits)Enum.Parse(typeof(StopBits), "One");
                ComPort.Handshake = (Handshake)Enum.Parse(typeof(Handshake), "None");
                ComPort.Parity = (Parity)Enum.Parse(typeof(Parity), "None");
                ComPort.Open();
                ComPort.DataReceived += new SerialDataReceivedEventHandler(mySerialPort_DataReceived);
                if (ComPort.IsOpen)
                {
                    button2.Text = "Connected";
                    button2.Enabled = false;
                    ComPort.ReceivedBytesThreshold = 17;
                    textBox1.AppendText("Connected" + "\n");
                    outThread.Start();
                    connected = true;
                    timer2.Start();
                    timerGo = true;
                }
            }
            else if (button2.Text == "Open")
            {
                button2.Text = "Closed";
                ComPort.Close();
            }
        }

        private void label3_Click(object sender, EventArgs e) { }
        private void label6_Click(object sender, EventArgs e) { }
        
        private void button3_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            DialogResult result = fbd.ShowDialog();
            DIR = result.ToString();
        }

        void writeToFile()
        {
            string fileName = "training";
            string outfile = "outfile";
            fileName += ".csv";
            outfile += ".csv";

            if(input == "Up")
            {
                outputs += "1,-1,-1,-1,-1,-1";
            }
            if (input == "Down")
            {
                outputs += "-1,1,-1,-1,-1,-1";
            }
            if (input == "Left")
            {
                outputs += "-1,-1,1,-1,-1,-1";
            }
            if (input == "Right")
            {
                outputs += "-1,-1,-1,1,-1,-1";
            }
            if (input == "Click")
            {
                outputs += "-1,-1,-1,-1,1,-1";
            }
            if (input == "Nothing")
            {
                outputs += "-1,-1,-1,-1,-1,1";
            }
            string full = Path.Combine(DIR, fileName);
            string out1 = Path.Combine(DIR, outfile);
            using (StreamWriter file = File.AppendText(full))
            {
                file.WriteLine(toFile);
                Console.WriteLine("file written");
                toFile = "";
            }
            using (StreamWriter file = File.AppendText(out1))
            {
                file.WriteLine(outputs);
                Console.WriteLine("file written");
                outputs = "";
            }
        }

        private void StartButton_Click(object sender, EventArgs e)
        {
            timerGo = true;
            timer1.Start();
            trials = Convert.ToInt32(comboBox4.SelectedItem);
            input = comboBox5.SelectedItem.ToString();
            StartButton.Enabled = false;
            startTime = DateTime.Now;
            Indicator.Text = "Wait...";
        }

        private void Form1_Load(object sender, EventArgs e) { }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            //Connection quality indicators
            if (connected)
            {
                if (c1_good)
                {
                    C1_IND.BackColor = Color.Green;
                }
                else
                {
                    C1_IND.BackColor = Color.Red;
                }
                if (c2_good)
                {
                    C2_IND.BackColor = Color.Green;
                }
                else
                {
                    C2_IND.BackColor = Color.Red;
                }
            }
        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            stopOutThread = true;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (timerGo)
            {
                //Console.WriteLine("timer ticking. timeleft: " + timeLeft);
                timeLeft = timeLeft - (float)(DateTime.Now - startTime).TotalSeconds;
                //kicks in when timer runs out
                if(timeLeft <= 0) {
                    if (trials > 0)
                    {
                        if (!record)
                        {
                            packetCount = 0;
                            count = 0;
                            ComPort.DiscardInBuffer();
                            record = true;
                            c1_errors = c2_errors = 0;
                            reject = false;
                            Indicator.BackColor = Color.Green;
                            Indicator.Text = "Recording...";
                            timeLeft = 60;
                            startTime = DateTime.Now;
                        }
                        else
                        {
                            record = false;
                            if (!reject)
                            {
                                writeToFile();
                                //reset errors
                                c1_errors = c2_errors = 0;
                                reject = false;
                                textBox1.AppendText("Trial Accepted" + "\n");
                            }
                            else //if either c1 or c2 errors exceed rejectT
                            {
                                //don't write to file, but reset strings
                                //reset errors
                                c1_errors = c2_errors = 0;
                                reject = false;
                                toFile = "";
                                outputs = "";
                                textBox1.AppendText("Trial Rejected" + "\n");

                            }
                            --trials;
                            Indicator.BackColor = Color.Red;
                            Indicator.Text = "Wait...";
                            timeLeft = 60;
                            startTime = DateTime.Now;
                        }
                    }
                    else
                    {
                        timer1.Stop();
                        StartButton.Enabled = true;
                        record = false;
                        Indicator.Text = "";
                        timeLeft = 60;
                    }
                }
            }
        }
    }
}
