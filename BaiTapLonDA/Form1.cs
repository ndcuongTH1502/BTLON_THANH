using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.Sockets;
using System.Net;
using System.Threading;

namespace BaiTapLonDA
{
    public partial class Form1 : Form
    {
        Socket cA;
        Socket cB;
        IPEndPoint ipepA = new IPEndPoint(IPAddress.Any, 1234);
        IPEndPoint ipepB = new IPEndPoint(IPAddress.Loopback, 1234);
        Thread Thread = null;
        int duration = 15;
        AES_256 AES_Check = new AES_256();
        public string dt = DateTime.Now.ToString();
        byte[] data = new byte[1024 * 24], publicKey, publicKeyC2, guiText, ak;
        Diffie_Hellman diffieHellman = new Diffie_Hellman();
        StringHex HexString = new StringHex();
        private void Form1_Load(object sender, EventArgs e)
        {
            cA = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            //cA.Bind(ipepA);
            cA.Listen(4);
            cA.BeginAccept(new AsyncCallback(CallAccept), cA);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            string Key = txtKey.Text;
            string Text = txtText.Text;
            string encText = AES_Check.Encrypt(Key, Text, dt);
            if (txtText.Text == "")
            {
                MessageBox.Show("Write something in others to run the program!");
            }
            else
            {
                int paddingto = PaddingValue();
                string _padding = paddingto.ToString();

                timer1.Enabled = true;
                timer1.Start();
                duration = 15;
                //Tạo publickey cho client A
                publicKey = diffieHellman.generatePublicKey();
                string _publicKey = Convert.ToBase64String(publicKey);
                //this.publicKey = this.diffieHellman.PublicKey.ToByteArray();

                ak = diffieHellman.secretKey(publicKey);
                string _a = Convert.ToBase64String(ak);

                //Gửi cho Client B
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
                richTextBox1.Text += "\nServer: " + txtText.Text;
                string text = encryptedText + ";" + dt + ";" + md5EncryptText + ";" + _publicKey + ";" + _padding + ";" + _a;
                guiText = Encoding.UTF8.GetBytes(text);

                //Bắt đầu gửi
                cB.BeginSend(guiText, 0, guiText.Length, SocketFlags.None, new AsyncCallback(SendData), cB);

                txtText.Clear();
                if (txtKey.Text == "")
                {
                    txtKey.Text = HexString.SHA_256(Convert.ToBase64String(ak));                
                }
            }
        }

       
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
                MessageBox.Show("Write something in others to run the program!");
            }
            else
            {
                int paddingto = PaddingValue();
                string _padding = paddingto.ToString();

                timer1.Enabled = true;
                timer1.Start();
                duration = 15;
                //Tạo publickey cho client A
                publicKey = diffieHellman.generatePublicKey();
                string _publicKey = Convert.ToBase64String(publicKey);

                ak = diffieHellman.secretKey(publicKey);
                string _a = Convert.ToBase64String(ak);
                guiText = new byte[1024 * 24];
                string encryptedText = encText;
                string md5EncryptText = MD5.maHoaMd5(encryptedText);
                txtEncrypt.Text = encryptedText;
                richTextBox1.Text += "\nServer: " + txtText.Text;
                string text = encryptedText + ";" + dt + ";" + md5EncryptText + ";" + _publicKey + ";" + _padding + ";" + _a;
                guiText = Encoding.UTF8.GetBytes(text);

                //Bắt đầu gửi
                cB.BeginSend(guiText, 0, guiText.Length, SocketFlags.None, new AsyncCallback(SendData), cB);

                txtText.Clear();

                if (txtKey.Text == "")
                {
                    txtKey.Text = HexString.SHA_256(Convert.ToBase64String(ak));
                }
            }
        }
        private void CallAccept(IAsyncResult i)
        {
            cB = ((Socket)i.AsyncState).EndAccept(i);
            Thread = new Thread(new ThreadStart(nhanDuLieu));
            Thread.Start();
        }
        void nhanDuLieu()
        {
            while (true)
            {
                if (cB.Poll(1000000, SelectMode.SelectRead))
                {
                    cB.BeginReceive(data, 0, data.Length, SocketFlags.None, new AsyncCallback(ReceiveData), cB);
                }
            }
        }
        private void SendData(IAsyncResult i)
        {
            cB = (Socket)i.AsyncState;
            int sent = cB.EndSend(i);
        }
        private void ReceiveData(IAsyncResult i)
        {
            cB = (Socket)i.AsyncState;
            int rec = cB.EndReceive(i);
            //Nhận dữ liệu từ client B
            string s = Encoding.ASCII.GetString(data, 0, rec);
            string[] arrString = s.Split(';');
            string encryptedText = arrString[0];
            string iv = arrString[1];
            string md5EncryptedText = arrString[2];
            string publicKey1 = arrString[3];
            string PaddingValue = arrString[4];
            publicKeyC2 = Convert.FromBase64String(publicKey1);
            string rawText = Decrypted(encryptedText, iv);
            richTextBox1.Invoke((MethodInvoker)delegate ()
            {
                richTextBox1.Text += "\nClient: " + rawText;
                label6.Text = PaddingValue;
            }
            );
            //So sánh chuỗi md5 để phát hiện gói tin có bị thay đổi hay không
            string hashText = MD5.maHoaMd5(encryptedText);

            if (md5EncryptedText != hashText)
            {
                richTextBox1.Invoke((MethodInvoker)delegate ()
                {
                    richTextBox1.Text += "\nClient: Content changed!";
                }
                );
            }
        }
        // Time
        private void timer1_Tick_1(object sender, EventArgs e)
        {
            duration--;
            textBox4.Text = duration.ToString();
            timer1.Interval = 1000;
            if (duration == 0)
            {
                timer1.Stop();
                MessageBox.Show("Session Time Out!");
                txtKey.Text = HexString.SHA_256(Convert.ToBase64String(ak));
                txtEncrypt.Clear();               
                duration = 15;
            }
        }

        public static string PhatSinhNgauNhienKyTu()
        {
            char[] chars = "$%#@!*abcdefghijklmnopqrstuvwxyz1234567890?;:ABCDEFGHIJKLMNOPQRSTUVWXYZ^&".ToCharArray();
            Random r = new Random();
            int i = r.Next(chars.Length);
            return chars[i].ToString();
        }
        public string Decrypted(string TextMaHoa, string Time)
        {
            string encryptedText = TextMaHoa;
            string key = txtKey.Text;
            string rawText = AES_Check.Decrypt(key, encryptedText, Time);
            return rawText;
        }
        //Padding dữ liệu
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
    }
}
