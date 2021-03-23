using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using System.Net.Security;
using System.IO;

namespace MutiServer
{
    public partial class Form1 : Form
    {   
        // used elements
        TcpListener serverSocket = null;
        TcpClient clientSocket = null;
        BackgroundWorker bgwServersend, bgwServerread;
        int port = 12345, range = 20, flag = 0, test = 0, win = 0, timecount = 0;
        string msg = "----";
        string control = "----";
        string filename = @"C:\FFOutput\";
        string union, name;
        const int BOARDSIZE = 10;
        const int BOARDLENGTH = 330;
        int[,] chessMap = new int[BOARDSIZE, BOARDSIZE];
        int start = 0, ambiguous = 5, paintx = 0, painty = 0, first = 0;
        int wins = 0, loses = 0, round = 0, left = 30, epic = 0;
        Random rnd = new Random();
        Graphics g;
        Bitmap bmp1, bmp2 = new Bitmap(@"C:\FFOutput\newboard.bmp", true);
        SolidBrush blackBrush = new SolidBrush(Color.Black);
        SolidBrush whiteBrush = new SolidBrush(Color.White);
        Thread thread, thread2;
        System.Timers.Timer time, timer;
        public delegate void MyInvoke(string param1);
        public delegate void sInvoke(string param2);

        public Form1()
        {
            InitializeComponent();
            set();
        }
        public void set()
        {
            // Set up the chess map and timer
            InitialChess();
            time = new System.Timers.Timer(1000);
            timer = new System.Timers.Timer(1000);
        }
        public void file()
        {
            /*
            Read the record file according to client's name and show it on the window.
            If the client's name is new to Server, then create a new file with client's name.
            */
            int final = msg.IndexOf('\0');
            char[] mm = msg.ToCharArray();
            char[] res = new char[final];
            for(int j = 0; j < final; ++j) { res[j] = mm[j]; }
            name = new string(res);

            union = filename + name;
            label6.Text = union;
            bool b = File.Exists(union);
            if (!b)
            {
                string createText = "0 0" + Environment.NewLine;
                File.WriteAllText(union, createText);
            }
            StreamReader sr = new StreamReader(union);
            char[] delimiterChars = {' '};
            while (!sr.EndOfStream)
            {

                string line = sr.ReadLine();
                string[] words = line.Split(delimiterChars);
                wins = Int32.Parse(words[0]);
                loses = Int32.Parse(words[1]);
            }
            sr.Close();
            round = wins + loses;
            label6.Text = "Client Record: W= " + wins + " L= " + loses + " S= " + round;
        }
        private void pictureBox1_Paint_1(object sender, PaintEventArgs e)
        {
            // Paint a white or black piece onto picturebox
            bmp1 = bmp2;
            pictureBox1.Image = bmp1;
            g = Graphics.FromImage(pictureBox1.Image);
            if (flag == 1)
            {
                g.FillEllipse(whiteBrush, paintx*30-10, painty*25-10 , range, range);
                chessMap[paintx-1, painty-1] = 1;
                flag = 0;
            }
            else if(flag == 2)
            {
                g.FillEllipse(blackBrush, paintx * 30 - 10, painty * 25 - 10, range, range);
                chessMap[paintx - 1, painty - 1] = 2;
                flag = 0;
            }
            bmp2 = bmp1;
        }
        public void InitialChess() 
        {
            // Initialize the chess map
            for (int i = 0; i < BOARDSIZE; i++)
            {
                for (int j = 0; j < BOARDSIZE; j++)
                {
                    chessMap[i, j] = 0;
                }
            }
        }
        public void DrawBoard(Graphics gg) 
        {
            // Draw the board. Not used anymore after producing newboard.bmp
            Pen p = new Pen(Brushes.Black, 3.0f);
            for (int i = 0; i <= BOARDSIZE; i++)
            {
               
                gg.DrawLine(p, new Point(0, (i + 1) * 25), new Point(BOARDLENGTH, (i + 1) * 25));
            }
            for (int i = 0; i <= BOARDSIZE; i++)
            {
               
                gg.DrawLine(p, new Point((i + 1) * 30, 0), new Point((i + 1) * 30, BOARDLENGTH));
            }
        }
        private void pictureBox1_MouseClick(object sender, MouseEventArgs e) 
        {
            // Get user's click and determine the position of the piece
            if (test != 1) return;

            int xx = e.X, yy = e.Y;
            int row = xx / 30;
            int col = yy / 25;
            int ambx = xx % 30;
            int amby = yy % 25;
            int xmax = 30 - ambiguous;
            int ymax = 25 - ambiguous;

            if(ambx <= ambiguous && amby <= ambiguous)
            {
                if(xx < 30 || yy < 25 || xx > 300 || yy > 250){ }
                else drawpoint(row-1, col-1);
            }
            else if(ambx <= ambiguous && amby >= ymax)
            {
                if (xx < 30 || yy < 25 || xx > 300 || yy > 250) { }
                else drawpoint(row-1, col);
            }
            else if(ambx >= xmax && amby <= ambiguous)
            {
                if (xx < 30 || yy < 25 || xx > 300 || yy > 250) { }
                else drawpoint(row, col-1);
            }
            else if (ambx >= xmax && amby >= ymax)
            {
                if (xx < 30 || yy < 25 || xx > 300 || yy > 250) { }
                else drawpoint(row, col);
            }

            
        }
        public void drawpoint(int x, int y) 
        {
            // To draw a point on picturebox
            if (chessMap[x,y] == 0)
            {
                paintx = x + 1;
                painty = y + 1;
                flag = 1;
                test = 2;
                chessMap[x , y ] = 1;
                pictureBox1.Refresh();
                package();
            }
        }
        public void package()
        {
            // Prepare to send a message to the client and change the status
            if(epic == 1)
            {
                bgwServersend.RunWorkerAsync();
                epic = 0;
            }
            else
            {
                judge();
                if (win == 1) { start = 3; changecontrol(3, start); }
                else if (win == 2) { start = 4; changecontrol(3, start); }
                else if (win == 0) start = 2;
                changecontrol(1, paintx - 1);
                changecontrol(2, painty - 1);
                bgwServersend.RunWorkerAsync();
            }
        }
        public void changecontrol(int pos, int num)
        {   
            // Changing status and record it into message
            char[] chars = control.ToCharArray();
            string str = Convert.ToString(num);
            char a = Convert.ToChar(str);
            chars[pos] = a;
            control = new string(chars);
        }
        public void Server()
        {
            // Build a TCP connection
            string forshow;
            int cont = rnd.Next(1,10);
            label1.Text = "Wait for a client";
            label1.Refresh();
            serverSocket = new TcpListener(IPAddress.Any, port);
            serverSocket.Start();
            while (true)
            {
                clientSocket = serverSocket.AcceptTcpClient();
                if (clientSocket != null) break;
            }
            // Show client's information after connection
            SslStream sslStream = new SslStream(clientSocket.GetStream(), true);
            IPEndPoint cpoint = clientSocket.Client.RemoteEndPoint as IPEndPoint;
            string cip = cpoint.Address.ToString();
            string cport = cpoint.Port.ToString();
            label2.Text = "Client IP: " + cip;
            label2.Refresh();
            label3.Text = "Client Port: " + cport;
            label3.Refresh();
            label1.Text = "Client Accepted";
            label1.Refresh();
            sslStream.Flush();
            timer.Elapsed += OnTimedEvent2;
            time.Elapsed += OnTimedEvent;
            time.Start();
            
            // Begin the game
            if (cont % 2 == 1) { start = 1; forshow = "MS: You go first!"; }
            else { start = 2; forshow = "MS: You go after client"; }
            Intbgw();
            bgwServerread.RunWorkerAsync();
            changecontrol(0,start);
            bgwServersend.RunWorkerAsync();
        }
        public void Intbgw()
        {
            // Start backgroundworker to handel read/write events and their after-effects
            bgwServerread = new BackgroundWorker();
            bgwServersend = new BackgroundWorker();
            bgwServerread.DoWork += new DoWorkEventHandler(bgwServerDoRead);
            bgwServerread.RunWorkerCompleted += new RunWorkerCompletedEventHandler(ReadCom);
            bgwServersend.DoWork += new DoWorkEventHandler(bgwServerDoSend);
            bgwServersend.RunWorkerCompleted += new RunWorkerCompletedEventHandler(SendCom);
        }
        public void bgwServerDoRead(object sender, DoWorkEventArgs e)
        {   
            // Read stream to buffer
            try
            {
                NetworkStream networkStream = clientSocket.GetStream();
                byte[] buffer = new byte[100];
                int count = 0;
                if ((count = networkStream.Read(buffer, 0, buffer.Length)) != 0)
                {
                    msg = Encoding.ASCII.GetString(buffer);
                    networkStream.Flush();
                }
            }
            catch (Exception ex)
            {
                //Console.WriteLine(ex.ToString());
            }
        }
        public void ReadCom(object sender, RunWorkerCompletedEventArgs e)
        {   
            /*
            After reading from the stream, check the current status.
            If it is just connecting, then show the client's name and go to next status. 
            Otherwise, decode the message and update the chess map and check if someone wins.
            */
            if (e.Error == null && !e.Cancelled)
            {
                if(first == 0)
                {
                    file();
                    string mmsg = name.Remove(name.Length - 4,4);
                    label4.Text = "Client Name: " + mmsg;
                    first = 1;
                    msg = "----";
                }
                else
                {
                    char[] chars = msg.ToCharArray();
                    msg = "----";
                    if (chars[1] == 'G' && chars[2] == 'G')
                    {
                        start = 1;
                        controller();
                    }
                    else
                    {
                        paintx = chars[1] - '0' + 1;
                        painty = chars[2] - '0' + 1;
                        flag = 2;
                        chessMap[paintx - 1, painty - 1] = 2;
                        pictureBox1.Refresh();

                        judge();
                        if (win == 1) start = 5;
                        else if (win == 2) start = 6;
                        else if (win == 0) start = 1;
                        controller();
                    }
                }
            }
        }
        public void bgwServerDoSend(object sender, DoWorkEventArgs e)
        {   
            // Send encoded message to client
            try
            {
                NetworkStream netStream = clientSocket.GetStream();
                string ss = control;
                control = "----";
                byte[] sendByte = Encoding.ASCII.GetBytes(ss);
                netStream.Write(sendByte, 0, sendByte.Length);
                netStream.Flush();
            }
            catch (Exception ex)
            {
                //Console.WriteLine(ex.ToString());
            }
        }
        public void SendCom(object sender, RunWorkerCompletedEventArgs e)
        {   
            // After sending messages, go back to main control
            if (e.Error == null && !e.Cancelled)
            {
                if(start == 1 || start == 2 || start == 3 || start == 4)controller();
            }
        }
        public void UpdateForm2(string param2)
        {
            // Update the window when countdown timer is running and make end to Server's turn when timer counts to zero
            left--;
            label7.Text = "Timeleft: " + left + param2;
            label7.Refresh();
            if (left == 0)
            {
                test = 2;
                start = 2;
                char[] chararr = control.ToCharArray();
                chararr[1] = 'G';
                chararr[2] = 'G';
                control = new string(chararr);
                epic = 1;
                package();
            }
        }
        public void OnTimedEvent2(object source, System.Timers.ElapsedEventArgs e)
        {   
            // Start a thread for countdown timer
            // thread2 = new Thread(new ThreadStart(DoWork2));
            // thread2.Start();
            // Use Invoke to update the window
            sInvoke mi2 = new sInvoke(UpdateForm2);
            Invoke(mi2, new Object[] { " seconds" });
        }
        public void UpdateForm(string param1)
        {   
            // Update the window when the timer is running
            timecount++;
            label5.Text ="Total: " +timecount + param1;
            label5.Refresh();
        }
        public void OnTimedEvent(Object source, System.Timers.ElapsedEventArgs e)
        {
            // Start a thread for the timer
            // thread = new Thread(new ThreadStart(DoWork));
            // thread.Start();
            // Use Invoke to update the window
            MyInvoke mi = new MyInvoke(UpdateForm);
            Invoke(mi, new Object[] {" seconds"});
        }
        public void write(int der)  
        {
            // Write the result to file and show it on both sides
            StreamWriter sw = new StreamWriter(union);
            if (der == 1)
            {
                loses += 1;
                string ww = wins.ToString();
                string ll = loses.ToString();
                string rr = ww + " " + ll;
                sw.WriteLine(rr);
                sw.Close();
                round = wins + loses;
                label6.Text = "Client Record: W= " + wins + " L= " + loses + " S= " + round;
                label6.Refresh();
            }
            else if(der == 2)
            {
                wins += 1;
                string ww = wins.ToString();
                string ll = loses.ToString();
                string rr = ww + " " + ll;
                sw.WriteLine(rr);
                sw.Close();
                round = wins + loses;
                label6.Text = "Client Record: W= " + wins + " L= " + loses + " S= " + round;
                label6.Refresh();
            }
        }
        public void controller() 
        {
            // Check the current status and determine what's next
            if (start == 1) 
            {
                label1.Text = "Your turn!";
                label1.Refresh();
                test = 1;
                timer.Start();
            }
            else if(start == 2) 
            {
                label1.Text = "Wait for your opponent!";
                label1.Refresh();
                timer.Stop();
                left = 30;
                bgwServerread.RunWorkerAsync();
            }
            else if(start == 3)
            {
                label1.Text = "You win!";
                label1.Refresh();
                test = 3;
                write(1);
                serverSocket.Stop();
                clientSocket.Close();
                time.Stop();
                timer.Stop();
                time.Dispose();
                timer.Dispose();

            }
            else if(start == 4)
            {
                label1.Text = "You lose!";
                label1.Refresh();
                test = 4;
                write(2);
                serverSocket.Stop();
                clientSocket.Close();
                time.Stop();
                timer.Stop();
                time.Dispose();
                timer.Dispose();

            }
            else if(start == 5)
            {
                label1.Text = "You win!";
                label1.Refresh();
                changecontrol(3, 5);
                bgwServersend.RunWorkerAsync();
                test = 5;
                write(1);
                serverSocket.Stop();
                clientSocket.Close();
                time.Stop();
                timer.Stop();
                time.Dispose();
                timer.Dispose();
            }
            else if(start == 6)
            {
                label1.Text = "You lose!";
                label1.Refresh();
                changecontrol(3, 6);
                bgwServersend.RunWorkerAsync();
                test = 6;
                write(2); 
                serverSocket.Stop();
                clientSocket.Close();
                time.Stop();
                timer.Stop();
                time.Dispose();
                timer.Dispose();
            }
        }
        public void judge() 
        {
            //Judge if someone wins the game by scanning the chess map
            int counter = 1, rec = 0;
            for(int i = 0; i < 10; ++i)
            {
                for(int j = 0; j < 10; ++j)
                {
                    if(chessMap[i,j] == 1)
                    {
                        for (int k = 1;  k < 5; ++k)
                        {
                            if (j + k < 10 && chessMap[i, j + k] == 1) counter++;
                        }
                        if (counter == 5) { rec = 1; break; }
                        else counter = 1;
                        for (int k = 1; k < 5; ++k)
                        {
                            if (i + k < 10 && chessMap[i + k, j] == 1) counter++;
                        }
                        if (counter == 5) { rec = 1; break; }
                        else counter = 1;
                        for (int k = 1; k < 5; ++k)
                        {
                            if (i + k < 10 && j + k < 10 && chessMap[i + k, j + k] == 1) counter++;
                        }
                        if (counter == 5) { rec = 1; break; }
                        else counter = 1;
                        for (int k = 1; k < 5; ++k)
                        {
                            if (i + k < 10 && j - k >= 0 && chessMap[i + k, j - k] == 1) counter++;
                        }
                        if (counter == 5) { rec = 1; break; }
                        else counter = 1;
                    }
                    else if (chessMap[i, j] == 2)
                    {
                        for (int k = 1; k < 5; ++k)
                        {
                            if (j + k < 10 && chessMap[i, j + k] == 2) counter++;
                        }
                        if (counter == 5) { rec = 2; break; }
                        else counter = 1;
                        for (int k = 1; k < 5; ++k)
                        {
                            if (i + k < 10 && chessMap[i + k, j] == 2) counter++;
                        }
                        if (counter == 5) { rec = 2; break; }
                        else counter = 1;
                        for (int k = 1; k < 5; ++k)
                        {
                            if (i + k < 10 && j + k < 10 && chessMap[i + k, j + k] == 2) counter++;
                        }
                        if (counter == 5) { rec = 2; break; }
                        else counter = 1;
                        for (int k = 1; k < 5; ++k)
                        {
                            if (i + k < 10 && j - k >= 0 && chessMap[i + k, j - k] == 2) counter++;
                        }
                        if (counter == 5) { rec = 2; break; }
                        else counter = 1;
                    }
                }
                if (rec == 1) { win = 1; break; }
                else if (rec == 2) { win = 2; break; }
            }
        }
        private void button3_Click(object sender, EventArgs e)  
        {
            // Start the Server
            button3.Enabled = false;
            Server();
            
        }
    }
}
