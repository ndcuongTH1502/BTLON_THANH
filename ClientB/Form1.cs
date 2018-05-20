using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace ClientB
{
    public partial class Form1 : Form
    {
        Socket cA;
        IPEndPoint ipepA = new IPEndPoint(IPAddress.Loopback, 1234);
        IPEndPoint ipepB = new IPEndPoint(IPAddress.Loopback, 1234);
        Thread Thread = null;
        int duration = 15;
        AES-256 AES_Check = new AES-256();
        public string dt = DateTime.Now.ToString();
        Diffie_Hellman2 Dman = new Diffie_Hellman2();
        byte[] data = new byte[1024 * 24], guiText, nhanPublicKey, sRecv, publicKey /*secretKey*/;
        StringToHex HexString = new StringToHex();
        public Form1()
        {
            InitializeComponent();
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            string Key = txtKey.Text;
            string Text = txtText.Text;
            string encText = AES_Check.Encrypt(Key, Text, dt);
            if (txtText.Text == "")
            {
                MessageBox.Show("Write something in other to run the program!");
            }
            else
            {
                int LengthPadding = PaddingValue();
                string _padding = LengthPadding.ToString();
                timer1.Enabled = true;
                timer1.Start();
                duration = 15;
                //Tạo publickey cho client B
                publicKey = Dman.generatePublicKey();
                string _publicKey = Convert.ToBase64String(publicKey);

                byte[] secretKey = Dman.secretKey(nhanPublicKey);

                //Gửi cho Client A
                guiText = new byte[1024 * 24];
                string encryptedText = encText;
                string md5EncryptText = MD5.maHoaMd5(encryptedText);
                txtEncrypt.Text = encryptedText;
                richTextBox1.Text += "\nClient: " + txtText.Text;

                string text = encryptedText + ";" + dt + ";" + md5EncryptText + ";" + _publicKey + ";" + _padding;
                guiText = Encoding.UTF8.GetBytes(text);

                //Bắt đầu gửi
                cA.BeginSend(guiText, 0, guiText.Length, SocketFlags.None, new AsyncCallback(SendData), c1);

                txtText.Clear();
                if (txtKey.Text == "")
                {
                    txtKey.Text = HexString.SHA_256(Convert.ToBase64String(sRecv));
                    //txtKey2.Text = Convert.ToBase64String(publicKey);
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            string Key = txtKey.Text;
            string Text = txtText.Text;
            string encText = AES_Check.Encrypt(Key, Text, dt);

            if (txtText.Text == "")
            {
                MessageBox.Show("Write something in other to run the program!");
            }
            else
            {
                int paddingto = PaddingValue();
                string _padding = paddingto.ToString();

                timer1.Enabled = true;
                timer1.Start();
                duration = 15;
                //Tạo publickey cho client B
                publicKey = Dman.generatePublicKey();
                string _publicKey = Convert.ToBase64String(publicKey);

                byte[] secretKey = Dman.secretKey(nhanPublicKey);

                //Gửi cho Client A
                guiText = new byte[1024 * 24];
                string encryptedText = encText;
                string a = encryptedText;
                int length = encryptedText.Length;
                Random r = new Random();
                int randomPos = r.Next(0, length + 1);
                string stringDauDenRandomPos = a.Substring(0, randomPos);
                string kyTuCuoiCuaText = a.Substring(randomPos);
                string textChanged = stringDauDenRandomPos + PhatSinhNgauNhienKyTu() + kyTuCuoiCuaText;
                string md5EncryptText = MD5.maHoaMd5(textChanged + txtKey.Text);
                txtEncrypt.Text = encryptedText;
                richTextBox1.Text += "\nClient: " + txtText.Text;
                string text = encryptedText + ";" + dt + ";" + md5EncryptText + ";" + _publicKey + ";" + _padding;
                guiText = Encoding.ASCII.GetBytes(text);

                //Bắt đầu gửi
                cA.BeginSend(guiText, 0, guiText.Length, SocketFlags.None, new AsyncCallback(SendData), c1);

                txtText.Clear();
                if (txtKey.Text == "")
                {
                    txtKey.Text = HexString.SHA_256(Convert.ToBase64String(sRecv));
                }
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            cA = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                cA.BeginConnect(ipepA, new AsyncCallback(Connected), cA);
            }
            catch (SocketException ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private void Connected(IAsyncResult i)
        {
            cA = ((Socket)i.AsyncState);
            cA.EndConnect(i);

            Thread = new Thread(new ThreadStart(nhanDuLieu));
            Thread.Start();
        }
        void nhanDuLieu()
        {
            while (true)
            {
                if (cA.Poll(1000000, SelectMode.SelectRead))
                {
                    cA.BeginReceive(data, 0, data.Length, SocketFlags.None, new AsyncCallback(ReceiveData), c1);
                }
            }
        }

        private void SendData(IAsyncResult i)
        {
            cA = (Socket)i.AsyncState;
            int sent = cA.EndSend(i);

        }
        private void ReceiveData(IAsyncResult i)
        {
            cA = (Socket)i.AsyncState;
            int rec = cA.EndReceive(i);
            //Nhận dữ liệu từ client 1
            string s = Encoding.ASCII.GetString(data, 0, rec);
            string[] arr = s.Split(';');
            string encryptedText = arr[0];
            string iv = arr[1];
            string md5EncryptedText = arr[2];
            string publicKey1 = arr[3];
            string Paddingvalue = arr[4];
            string SKey = arr[5];
            nhanPublicKey = Convert.FromBase64String(publicKey1);
            sRecv = Convert.FromBase64String(SKey);
            string rawText = Decrypted(encryptedText, iv);

            richTextBox1.Invoke((MethodInvoker)delegate ()
            {
                richTextBox1.Text += "\nServer: " + rawText;
                label6.Text = Paddingvalue;
            }
            );
            string hashText = MD5.maHoaMd5(encryptedText);
            if (md5EncryptedText != hashText)
            {

                richTextBox1.Invoke((MethodInvoker)delegate ()
                {
                    richTextBox1.Text += "\nServer: Content changed!";
                }
                );
            }
        }

        public static string PhatSinhNgauNhienKyTu()
        {
            char[] chars = "$%#@!*abcdefghijklmnopqrstuvwxyz1234567890?;:ABCDEFGHIJKLMNOPQRSTUVWXYZ^&".ToCharArray();
            Random r = new Random();
            int i = r.Next(chars.Length);
            return chars[i].ToString();
        }
        int PaddingValue()
        {
            string Time = DateTime.Now.ToString("HH:mm:ss");
            string MHTime = MD5.maHoaMd5(Time);
            int i = 0;
            string Length = null;
            if (txtText.TextLength % 16 != 0)
            {
                i = 1;
                int Temp = txtText.TextLength;
                while (Temp % 16 != 0)
                {
                    Length = MHTime.Substring(0, i);
                    Temp += 1;
                    i += 1;
                }
                txtText.Text += Length;
            }
            return i - 1;
        }
        public string Decrypted(string TextMaHoa, string Time)
        {
            string encryptedText = TextMaHoa;
            string key = txtKey.Text;
            string rawText = AES_Check.Decrypt(key, encryptedText, Time);
            return rawText;
        }
        
        private void timer1_Tick(object sender, EventArgs e)
        {
            duration--;
            textBox4.Text = duration.ToString();
            timer1.Interval = 1000;
            if (duration == 0)
            {
                timer1.Stop();
                MessageBox.Show("Session Time Out!");
                txtKey.Text = HexString.SHA_256(Convert.ToBase64String(sRecv));
                txtEncrypt.Clear();
                duration = 15;
            }
        }
    }
}
