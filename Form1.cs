using GMap.NET;
using GMap.NET.MapProviders;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using static arayüz_okuma.Form1;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
namespace arayüz_okuma
{
    public partial class Form1 : Form
    {
        public struct payload
        {
            public float bme_alt, bme_pres, bme_temp;
            public float gps_alt, gps_lat, gps_lon;
            public float gyro_x, gyro_y, gyro_z, acc_x, acc_y, acc_z;
            public float pl_angx, pl_angy, pl_angz;
        }
        public struct Rocket
        {
            public float bme_alt, bme_pres, bme_temp;
            public float gps_alt, gps_lat, gps_lon;
            public float gyro_x, gyro_y, gyro_z, acc_x, acc_y, acc_z;
            public float rc_angx, rc_angy, rc_angz, rc_angle;
            public int packetnbr;
            public float magx, magy, magz;
            public int rc_stat;
            public float speed;
        }
        string start_time = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
        StreamWriter log_file_astra;
        public byte[] package = new byte[78];
        public int checksum;
        Rocket rocket = new Rocket();
        payload Payload = new payload();
        bool hyi;
        int minm = 0;
        int maksm = 0;
        private Process _unityProcess;
        public float adress_lat = 38.3686F;
        public float adress_lon = 34.0297F;
        string similas;
        public Form1()
        {
            InitializeComponent();
            serialPort1.BaudRate = 9600;
            serialPort1.RtsEnable = true;
            serialPort1.DtrEnable = true;
            Control.CheckForIllegalCrossThreadCalls = false;
            this.FormClosing += Form1_FormClosing;

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            string log_path = Path.Combine(Application.StartupPath, start_time + "_astra_log.txt");
            if (!File.Exists(log_path))
            {
                File.Create(log_path).Close();
            }
            log_file_astra = new StreamWriter(log_path, true);
            gMapControl1.MapProvider = GMapProviders.GoogleMap;
            gMapControl1.Position = new PointLatLng(40.991734, 28.831467);
            StartUnityApplication();
            rocket.packetnbr = 0;
        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_unityProcess != null && !_unityProcess.HasExited)
            {
                _unityProcess.CloseMainWindow(); // Unity uygulamasını düzgün bir şekilde kapatmayı dener
                _unityProcess.WaitForExit(1000); // Kapanması için bekler (isteğe bağlı timeout)
                if (!_unityProcess.HasExited)
                {
                    _unityProcess.Kill(); // Hala kapanmamışsa zorla kapatır
                }
            }
        }
        static float RadiansToDegrees(double radians)
        {
            return (float)(radians * (180.0 / Math.PI));
        }
        private void serialPort1_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                // Veriyi oku
                string data = serialPort1.ReadLine();  // ReadLine ile veriyi oku

                if (string.IsNullOrEmpty(data))
                {
                    label27.ForeColor = Color.Red;
                    label27.Text = "Gelen veri boş";
                    return;
                }
                log_file_astra.Write(data);
                // Veriyi ',' karakterine göre ayır
                string[] splitdata = data.Split(',');
                // Veriyi ilgili kontrol elemanlarına yerleştir
                this.Invoke((MethodInvoker)delegate
                {
                    // splitdata dizisinin uzunluğunu kontrol et
                    if (splitdata.Length >= 14)  // Minimum uzunluk 19, veri formatınıza bağlı olarak 20'yi deneyin
                    {
                        try
                        {
                            // Verileri ekranda göster
                            label3.Text = data.Replace(",", "   ").Insert(95, "\n");
                            // Verileri parse et ve ekranda göster
                            if (splitdata[0] == "$AR")
                            {
                                rocket.acc_x = float.Parse(splitdata[1]);
                                rocket.acc_y = float.Parse(splitdata[2]);
                                rocket.acc_z = float.Parse(splitdata[3]);
                                rocket.gyro_x = float.Parse(splitdata[4]);
                                rocket.gyro_y = float.Parse(splitdata[5]);
                                rocket.gyro_z = float.Parse(splitdata[6]);
                                rocket.rc_angx = float.Parse(splitdata[7]);
                                rocket.rc_angy = float.Parse(splitdata[8]);
                                rocket.rc_angz = float.Parse(splitdata[9]);
                                rocket.gps_lat = float.Parse(splitdata[10]);
                                rocket.gps_lon = float.Parse(splitdata[11]);
                                rocket.gps_alt = float.Parse(splitdata[12]);
                                rocket.rc_angle = float.Parse(splitdata[13]);
                                rocket.bme_alt = float.Parse(splitdata[14]);
                                rocket.speed = float.Parse(splitdata[15]);
                                rocket.bme_temp = float.Parse(splitdata[16]);
                                rocket.bme_pres = float.Parse(splitdata[17]);
                                rocket.rc_stat = int.Parse(splitdata[18]);

                                accx_label.Text = splitdata[1];
                                accy_label.Text = splitdata[2];
                                accz_label.Text = splitdata[3];
                                gyrox_label.Text = splitdata[4];
                                gyroy_label.Text = splitdata[5];
                                gyroz_label.Text = splitdata[6];
                                rc_angx_label.Text = splitdata[7];
                                rc_angy_label.Text = splitdata[8];
                                rc_angz_label.Text = splitdata[9];
                                lat_label.Text = splitdata[10];
                                lon_label.Text = splitdata[11];
                                gps_alt_label.Text = splitdata[12];
                                bme_alt_label.Text = splitdata[14];
                                bme_tmp_label.Text = splitdata[16];
                                bme_pres_label.Text = splitdata[17];
                                drm.Text = splitdata[18];
                                angle.Text = splitdata[13];
                                label8.Text = splitdata[15];
                                // Paket numarasını güncelle ve etikette göster

                                label2.Text = rocket.packetnbr.ToString();
                                rocket.packetnbr += 1;
                                //similas = splitdata[7] + "," + splitdata[8] + "," + splitdata[9];
                                similas = RadiansToDegrees(rocket.rc_angz).ToString() + "," +
                                (-RadiansToDegrees(rocket.rc_angy)).ToString() + "," +
                                RadiansToDegrees(rocket.rc_angx).ToString();

                                // X ekseni ayarları

                                this.chart1.Series[0].Points.AddXY(maksm, rocket.bme_pres);

                                chart1.ChartAreas[0].AxisX.Minimum = 0;
                                chart1.ChartAreas[0].AxisX.Maximum = maksm;
                                chart1.ChartAreas[0].AxisY.Minimum = 0;
                                //chart1.ChartAreas[0].AxisY.Maximum = 120000;
                                // Mevcut maksimum Y ekseni değeri (float olarak)
                                float currentYAxisMax = (float)chart1.ChartAreas[0].AxisY.Maximum;


                                // Yeni veri noktası ekle
                                // Grafik serisinin sınır genişliği
                                //chart1.Series["Series1"].BorderWidth = 3;
                                //  chart1.Series["Series1"].BorderColor = Color.FromArgb(255, 0, 0); // Kırmızı renk

                                this.chart2.Series[0].Points.AddXY(maksm, rocket.rc_angle);
                                chart2.ChartAreas[0].AxisX.Minimum = 0;
                                chart2.ChartAreas[0].AxisX.Maximum = maksm;
                                chart2.ChartAreas[0].AxisY.Minimum = 0;
                                //chart2.ChartAreas[0].AxisY.Maximum = 180;
                                chart2.ChartAreas[0].AxisX.ScaleView.Zoom(minm, maksm);



                                this.chart3.Series[0].Points.AddXY(maksm, rocket.bme_alt);


                                chart3.ChartAreas[0].AxisX.Minimum = 0;
                                chart3.ChartAreas[0].AxisX.Maximum = maksm;
                                chart3.ChartAreas[0].AxisY.Minimum = 0;
                                //chart3.ChartAreas[0].AxisY.Maximum = rocket.bme_alt;

                                chart3.ChartAreas[0].AxisX.ScaleView.Zoom(minm, maksm);
                                // chart4.ChartAreas[0].AxisX.Minimum = 0;
                                // chart4.ChartAreas[0].AxisX.Maximum = maksm;
                                // chart4.ChartAreas[0].AxisY.Minimum = 0;
                                // chart4.ChartAreas[0].AxisY.Maximum = 1200;
                                // chart4.ChartAreas[0].AxisX.ScaleView.Zoom(minm, maksm);
                                // this.chart4.Series[0].Points.AddXY(maksm/2 , payload.bme_alt);
                                maksm++;
                                // Veriyi Unity'e gönder
                                gMapControl1.Position = new PointLatLng(rocket.gps_lat, rocket.gps_lon);

                                SendDataToUnity(similas);
                                string basePath = AppDomain.CurrentDomain.BaseDirectory;
                                string txtPath = Path.Combine(basePath, "Astra_Rocket_Log.txt");

                                using (StreamWriter writer = new StreamWriter(txtPath, true))
                                {
                                    writer.WriteLine(data);
                                }

                            }
                            label27.ForeColor = Color.Green;
                            label27.Text = "başarılı";

                            // Rocket objesini güncelle
                        }
                        catch (Exception ex)
                        {
                            // Verilerin işlenmesi sırasında oluşan hataları yakala
                            label27.ForeColor = Color.Red;
                            label27.Text = "Veri işlenirken hata oluştu";
                        }
                    }
                    else
                    {
                        // Hata mesajı göster veya işleme yap
                        label27.ForeColor = Color.Red;
                        label27.Text = "Gelen veri formatı hatalı veya eksik.";
                    }
                });
            }
            catch (Exception ex)
            {
                // Genel hata mesajı
                label27.ForeColor = Color.Red;
                label27.Text = $"Hata: {ex.Message}";

            }
        }
        private void button1_Click(object sender, EventArgs e)
        {
            start_time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            if (!serialPort1.IsOpen)
            {
                try
                {
                    serialPort1.PortName = comboBox1.Text;

                    try
                    {
                        serialPort1.Open();

                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Seri port açılamadı: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    // Veri alım olayına dinleyici ekle
                    serialPort1.DataReceived += new SerialDataReceivedEventHandler(serialPort1_DataReceived);

                }
                catch
                {
                }
            }
        }
        private void SendDataToUnity(string simila)
        {
            UdpClient udpClient = new UdpClient();
            try
            {
                // Unity'deki UDP sunucusunun IP adresi ve port numarası
                string unityIP = "127.0.0.1"; // böyle iyi
                int unityPort = 6063;
                // Gönderilecek veri
                byte[] veri = Encoding.ASCII.GetBytes(simila);
                // Veriyi gönderin
                udpClient.Send(veri, veri.Length, unityIP, unityPort);
            }
            catch (Exception e)
            {
                MessageBox.Show("Send error: " + e.Message);
            }
            finally
            {
                udpClient.Close();
            }
        }
        public byte[] float_to_byte(float input_data)
        {
            return BitConverter.GetBytes(input_data);
        }
        private void button2_Click(object sender, EventArgs e)
        {
            serialPort1.Close();
        }
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
        }
        private void button3_Click(object sender, EventArgs e)
        {

            hyi = true;
        }
        private void button4_Click(object sender, EventArgs e)
        {
            serialPort2.Close();
            hyi = false;
        }
        private void timer1_Tick_1(object sender, EventArgs e)
        {
            if (hyi == true)
            {
                checksum = 0;
                package[0] = 0xFF; // Sabit
                package[1] = 0xFF; // Sabit
                package[2] = 0x54; // Sabit
                package[3] = 0x52; // Sabit
                package[4] = 114;
                package[5] = (byte)(rocket.packetnbr & 0xFF);
                package[6] = float_to_byte(rocket.bme_alt)[0];
                package[7] = float_to_byte(rocket.bme_alt)[1];
                package[8] = float_to_byte(rocket.bme_alt)[2];
                package[9] = float_to_byte(rocket.bme_alt)[3];
                package[10] = float_to_byte(rocket.gps_alt)[0];
                package[11] = float_to_byte(rocket.gps_alt)[1];
                package[12] = float_to_byte(rocket.gps_alt)[2];
                package[13] = float_to_byte(rocket.gps_alt)[3];
                package[14] = float_to_byte(rocket.gps_lat)[0];
                package[15] = float_to_byte(rocket.gps_lat)[1];
                package[16] = float_to_byte(rocket.gps_lat)[2];
                package[17] = float_to_byte(rocket.gps_lat)[3];
                package[18] = float_to_byte(rocket.gps_lon)[0];
                package[19] = float_to_byte(rocket.gps_lon)[1];
                package[20] = float_to_byte(rocket.gps_lon)[2];
                package[21] = float_to_byte(rocket.gps_lon)[3];
                package[22] = float_to_byte(Payload.gps_alt)[0];
                package[23] = float_to_byte(Payload.gps_alt)[1];
                package[24] = float_to_byte(Payload.gps_alt)[2];
                package[25] = float_to_byte(Payload.gps_alt)[3];
                package[26] = float_to_byte(Payload.gps_lat)[0];
                package[27] = float_to_byte(Payload.gps_lat)[1];
                package[28] = float_to_byte(Payload.gps_lat)[2];
                package[29] = float_to_byte(Payload.gps_lat)[3];
                package[30] = float_to_byte(Payload.gps_lon)[0];
                package[31] = float_to_byte(Payload.gps_lon)[1];
                package[32] = float_to_byte(Payload.gps_lon)[2];
                package[33] = float_to_byte(Payload.gps_lon)[3];
                //Kademe yok bizde
                package[34] = 0;
                package[35] = 0;
                package[36] = 0;
                package[37] = 0;
                package[38] = 0;
                package[39] = 0;
                package[40] = 0;
                package[41] = 0;
                package[42] = 0;
                package[43] = 0;
                package[44] = 0;
                package[45] = 0;
                package[46] = float_to_byte(rocket.gyro_x)[0];
                package[47] = float_to_byte(rocket.gyro_x)[1];
                package[48] = float_to_byte(rocket.gyro_x)[2];
                package[49] = float_to_byte(rocket.gyro_x)[3];
                package[50] = float_to_byte(rocket.gyro_y)[0];
                package[51] = float_to_byte(rocket.gyro_y)[1];
                package[52] = float_to_byte(rocket.gyro_y)[2];
                package[53] = float_to_byte(rocket.gyro_y)[3];
                package[54] = float_to_byte(rocket.gyro_z)[0];
                package[55] = float_to_byte(rocket.gyro_z)[1];
                package[56] = float_to_byte(rocket.gyro_z)[2];
                package[57] = float_to_byte(rocket.gyro_z)[3];
                package[58] = float_to_byte(rocket.acc_x)[0];
                package[59] = float_to_byte(rocket.acc_x)[1];
                package[60] = float_to_byte(rocket.acc_x)[2];
                package[61] = float_to_byte(rocket.acc_x)[3];
                package[62] = float_to_byte(rocket.acc_y)[0];
                package[63] = float_to_byte(rocket.acc_y)[1];
                package[64] = float_to_byte(rocket.acc_y)[2];
                package[65] = float_to_byte(rocket.acc_y)[3];
                package[66] = float_to_byte(rocket.acc_z)[0];
                package[67] = float_to_byte(rocket.acc_z)[1];
                package[68] = float_to_byte(rocket.acc_z)[2];
                package[69] = float_to_byte(rocket.acc_z)[3];
                package[70] = float_to_byte(rocket.rc_angle)[0];
                package[71] = float_to_byte(rocket.rc_angle)[1];
                package[72] = float_to_byte(rocket.rc_angle)[2];
                package[73] = float_to_byte(rocket.rc_angle)[3];

                package[74] = (byte)(rocket.rc_stat & 0xFF);
                checksum = 0;
                for (int i = 4; i < 75; i++)
                    checksum += package[i];
                package[75] = (byte)(checksum % 256);
                package[76] = 0x0D;
                package[77] = 0x0A;
                serialPort2.Write(package, 0, 78);
                rocket.packetnbr++;
                if (rocket.packetnbr > 255)
                {
                    rocket.packetnbr = 0;
                }
            }
        }
        private void StartUnityApplication()
        {
            _unityProcess = new Process();
            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            string exePath = Path.Combine(basePath, "similasyon\\ozgunn.exe");
            _unityProcess.StartInfo.FileName = exePath;
            _unityProcess.StartInfo.Arguments = "-parentHWND " + panel4.Handle.ToInt32();
            _unityProcess.StartInfo.UseShellExecute = true;
            _unityProcess.Start();
            _unityProcess.WaitForInputIdle();
            //SetParent(_unityProcess.MainWindowHandle, panel1.Handle);
            SendMessage(_unityProcess.MainWindowHandle, WM_SYSCOMMAND, SC_MAXIMIZE, 0);
        }
        private const int WM_SYSCOMMAND = 0x0112;
        private const int SC_MAXIMIZE = 0xF030;
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, uint Msg, int wParam, int lParam);
        private void timer2_Tick(object sender, EventArgs e)
        {
        }
        private void gMapControl1_Load(object sender, EventArgs e)
        {
        }
        private void PORT_button_Click(object sender, EventArgs e)
        {
            comboBox1.Items.Clear();
            String[] ports = SerialPort.GetPortNames();
            foreach (var port in ports)
            {
                if (!comboBox1.Items.Contains(port))
                {
                    comboBox1.Items.Add(port);
                }
            }



        }
        private void serialPort3_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {


        }
        private void label14_Click(object sender, EventArgs e)
        {
        }
        private void label27_Click(object sender, EventArgs e)
        {
        }

        private void button5_Click(object sender, EventArgs e)
        {

        }

        private void button6_Click(object sender, EventArgs e)
        {

        }

        private void comboBox1_MouseClick(object sender, MouseEventArgs e)
        {
            comboBox1.Items.Clear();
            String[] ports = SerialPort.GetPortNames();
            foreach (var port in ports)
            {
                if (!comboBox1.Items.Contains(port))
                {
                    comboBox1.Items.Add(port);
                }
            }
        }

        private void button3_Click_1(object sender, EventArgs e)
        {
            maksm = 0;
            this.chart1.Series[0].Points.Clear();
            this.chart2.Series[0].Points.Clear();
            this.chart3.Series[0].Points.Clear();
        }
    }
}
