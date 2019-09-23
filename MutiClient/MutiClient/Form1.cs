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


namespace MutiClient
{
    public partial class Form1 : Form
    {
        TcpClient clientSocket = null;
        int port = 12345, range = 20, flag = 0, test = 0, first = 0, timecount = 0;
        int ambiguous = 5, paintx = 0, painty = 0, left = 30, epic = 0;
        bool connect = false;
        BackgroundWorker bgwClientsend, bgwClientread;
        string msg = "----";
        const int BOARDSIZE = 10;
        const int BOARDLENGTH = 330;
        int[,] chessMap = new int[BOARDSIZE, BOARDSIZE];
        string control = "----";
        string username = "";
        string txt = ".txt";
        Graphics g;
        Bitmap bmp1, bmp2 = new Bitmap(@"C:\FFOutput\newboard.bmp", true);
        SolidBrush blackBrush = new SolidBrush(Color.Black);
        SolidBrush whiteBrush = new SolidBrush(Color.White);
        System.Timers.Timer time, timer;
        Thread thread, thread2;
        public delegate void MyInvoke(string param1);
        public delegate void sInvoke(string param2);

        public Form1()
        {
            InitializeComponent();
            set();
        }
        public void set()
        {
            InitialChess();
            time = new System.Timers.Timer(1000);
            timer = new System.Timers.Timer(1000);
        }
        private void pictureBox1_Paint_1(object sender, PaintEventArgs e)
        {
            bmp1 = bmp2;
            pictureBox1.Image = bmp1;
            g = Graphics.FromImage(pictureBox1.Image);
            if (flag == 1)
            {
                g.FillEllipse(whiteBrush, paintx * 30 - 10, painty * 25 - 10, range, range);
                chessMap[paintx - 1, painty - 1] = 1;
                flag = 0;
            }
            else if (flag == 2)
            {
                g.FillEllipse(blackBrush, paintx * 30 - 10, painty * 25 - 10, range, range);
                chessMap[paintx - 1, painty - 1] = 2;
                flag = 0;
            }
            bmp2 = bmp1;
        }
        public void InitialChess()
        {
            for (int i = 0; i < BOARDSIZE; i++)
            {
                for (int j = 0; j < BOARDSIZE; j++)
                {
                    chessMap[i, j] = 0;
                }
            }
            //this.pictureBox1.Invalidate();
        }
        public void DrawBoard(Graphics g)
        {
            Pen p = new Pen(Brushes.Black, 3.0f);
            for (int i = 0; i <= BOARDSIZE; i++)
            {
                g.DrawLine(p, new Point(0, (i + 1) * 25), new Point(BOARDLENGTH, (i + 1) * 25));
            }
            for (int i = 0; i <= BOARDSIZE; i++)
            {
                g.DrawLine(p, new Point((i + 1) * 30, 0), new Point((i + 1) * 30, BOARDLENGTH));
            }
        }
        private void pictureBox1_MouseClick_1(object sender, MouseEventArgs e)
        {
            if (test != 2) return;

            int xx = e.X, yy = e.Y;
            int row = xx / 30;
            int col = yy / 25;
            int ambx = xx % 30;
            int amby = yy % 25;
            int xmax = 30 - ambiguous;
            int ymax = 25 - ambiguous;

            if (ambx <= ambiguous && amby <= ambiguous)
            {
                if (xx < 30 || yy < 25 || xx > 300 || yy > 250) { }
                else drawpoint(row-1, col-1);
            }
            else if (ambx <= ambiguous && amby >= ymax)
            {
                if (xx < 30 || yy < 25 || xx > 300 || yy > 250) { }
                else drawpoint(row-1, col);
            }
            else if (ambx >= xmax && amby <= ambiguous)
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
            if (chessMap[x , y] == 0)
            {
               
                paintx = x + 1;
                painty = y + 1;
                flag = 2;
                test = 1;
                chessMap[x , y ] = 2;
                pictureBox1.Refresh();
                package();
            }
        }
        public void package()
        {
            label1.Text = "Wait for your opponent!";
            label1.Refresh();
            if (epic == 1)
            {
                epic = 0;
                bgwClientsend.RunWorkerAsync();
            }
            else
            {
                changecontrol(1, paintx - 1);
                changecontrol(2, painty - 1);
                bgwClientsend.RunWorkerAsync();
            }
        }
        public void changecontrol(int pos, int num)
        {
            char[] chars = control.ToCharArray();
            string str = Convert.ToString(num);
            char a = Convert.ToChar(str);
            chars[pos] = a;
            control = new string(chars);
        }
        public void Client()
        {
            clientSocket = new TcpClient();
            try
            {
                clientSocket.Connect("127.0.0.1", port);
            }
            catch (Exception ex)
            {
                label1.Text = "Sever not found";
                clientSocket = null;
                return;
            }
            label1.Text = "Connect to Server";
            label1.Refresh();
            timer.Elapsed += OnTimedEvent2;
            time.Elapsed += OnTimedEvent;
            time.Start();

            connect = true;
            Intbgw();
            control = username + txt;
            bgwClientsend.RunWorkerAsync();
            bgwClientread.RunWorkerAsync();
        }
        public void Intbgw()
        {
            bgwClientsend = new BackgroundWorker();
            bgwClientread = new BackgroundWorker();
            bgwClientsend.DoWork += new DoWorkEventHandler(bgwClientDoSend);
            bgwClientread.DoWork += new DoWorkEventHandler(bgwClientDoRead);
            bgwClientread.RunWorkerCompleted += new RunWorkerCompletedEventHandler(ReadCom);
            bgwClientsend.RunWorkerCompleted += new RunWorkerCompletedEventHandler(SendCom);
        }
        public void bgwClientDoSend(object sender, DoWorkEventArgs e)
        {
            try
            {
                NetworkStream clientStream = clientSocket.GetStream();
                string ss = control;
                control = "----";
                byte[] outStream = Encoding.ASCII.GetBytes(ss);
                clientStream.Write(outStream, 0, outStream.Length);
                clientStream.Flush();
            }
            catch(Exception ex)
            {
                //Console.WriteLine(ex.ToString());
            }

        }
        public void bgwClientDoRead(object sender, DoWorkEventArgs e)
        {
            try
            {
                NetworkStream netStream = clientSocket.GetStream();
                byte[] inStream = new byte[1000];
                netStream.Read(inStream, 0, inStream.Length);
                msg = Encoding.ASCII.GetString(inStream);
                netStream.Flush();
            }
            catch (Exception ex)
            {
                //Console.WriteLine(ex.ToString());
            }
        }
        public void ReadCom(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error == null && !e.Cancelled)
            {
                
                char[] chars = msg.ToCharArray();
                msg = "----";
                int a = 0, b = 1, c = 3;
                if (chars[a] != '-') controller(1, chars);
                if (chars[b] != '-') controller(2, chars);
                if (chars[c] != '-') controller(3, chars);
            }
        }
        public void SendCom(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error == null && !e.Cancelled)
            {
                timer.Stop();
                left = 30;
                if (first == 1) { control = "----"; first = 0; }
                else bgwClientread.RunWorkerAsync();
            }
        }
        public void UpdateForm2(string param2)
        {
            left--;
            label5.Text = "Timeleft: " + left + param2;
            label5.Refresh();
            if (left == 0)
            {
                test = 1;
                char[] chararr = control.ToCharArray();
                chararr[1] = 'G';
                chararr[2] = 'G';
                control = new string(chararr);
                epic = 1;
                package();
            }
        }
        public void DoWork2()
        {
            sInvoke mi2 = new sInvoke(UpdateForm2);
            Invoke(mi2, new Object[] { " seconds" });
        }
        public void OnTimedEvent2(object source, System.Timers.ElapsedEventArgs e)
        {
            thread2 = new Thread(new ThreadStart(DoWork2));
            thread2.Start();
        }
        public void UpdateForm(string param1)
        {
            timecount++;
            label4.Text = "Total: " + timecount + param1;
            label4.Refresh();
        }
        public void DoWork()
        {
            MyInvoke mi = new MyInvoke(UpdateForm);
            Invoke(mi, new Object[] { " seconds" });
        }
        public void OnTimedEvent(Object source, System.Timers.ElapsedEventArgs e)
        {
            thread = new Thread(new ThreadStart(DoWork));
            thread.Start();
        }
        public void controller(int num, char[] schar)
        {
            if (num == 1)
            {
                if (schar[0] == '1')
                {
                    //MessageBox.Show("MC: You go after Server!");
                    label1.Text = "Wait for your opponent!";
                    label1.Refresh();
                    test = 1;
                    bgwClientread.RunWorkerAsync();

                }
                else if (schar[0] == '2')
                {
                    //MessageBox.Show("MC: You go first!");
                    label1.Text = "Your turn!";
                    label1.Refresh();
                    test = 2;
                    timer.Start();
                }
            }
            else if (num == 2)
            {
                paintx = schar[1] - '0' + 1;
                painty = schar[2] - '0' + 1;
                if (schar[1] == 'G' && schar[2] == 'G')
                {
                    test = 2;
                    label1.Text = "Your turn!";
                    label1.Refresh();
                    timer.Start();
                }
                else if (paintx < 1)
                {
                    test = 3;
                    label1.Text = "You win!";
                    label1.Refresh();
                    time.Stop();
                    timer.Stop();
                    time.Dispose();
                    timer.Dispose();
                    clientSocket.Close();
                }
                else
                {
                    flag = 1;
                    chessMap[paintx - 1, painty - 1] = 1;
                    pictureBox1.Refresh();
                    test = 2;
                    label1.Text = "Your turn!";
                    label1.Refresh();
                    timer.Start();
                }
                //檢驗是否分出勝負
            }
            else if (num == 3)
            {
                if(schar[3] == '3' || schar[3] == '5')
                {
                    test = 3;
                    label1.Text = "You lose!";
                    label1.Refresh();
                    time.Stop();
                    timer.Stop();
                    time.Dispose();
                    timer.Dispose();
                    clientSocket.Close();

                }
                else if(schar[3] == '4' || schar[3] == '6')
                {
                    test = 3;
                    label1.Text = "You win!";
                    label1.Refresh();
                    time.Stop();
                    timer.Stop();
                    time.Dispose();
                    timer.Dispose();
                    clientSocket.Close();
                }
            }
        }
        private void button3_Click(object sender, EventArgs e)
        {
            Client();
            if(connect)button3.Enabled = false;
            button3.Refresh();
        }
        private void button4_Click(object sender, EventArgs e)
        {
            username = textBox1.Text;
            label3.Text = "Your Name: " + username;
            label3.Refresh();
            textBox1.Enabled = false;
            button4.Enabled = false;
            first = 1;
        }
    }
}
