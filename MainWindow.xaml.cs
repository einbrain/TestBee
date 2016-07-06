using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace TestBee
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const Int16 CAM_WIDTH = 640;
        private const Int16 CAM_HEIGHT = 480;
        private const Int16 KINECT_WIDTH = 640;
        private const Int16 KINECT_HEIGHT = 480;

        private Double dPadWidth;
        private Double dPadHeight;

        private Boolean bTestIsRunning;
        private Boolean bPaused;
        private Boolean bExitPending;
        private Boolean bLaserLoop;
        private Boolean bHandLoop;
        private Boolean bLMBLoop;

        private Boolean bMouseMoving;

        private Point mousePos;

        private UdpClient udp;
        private IPEndPoint ipep;

        private Thread tLaser, tHand, tLMB;
        private int nLaserWaitTime, nHandWaitTime, nLMBWaitTime;

        private FaceDetector fd;


        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            dPadWidth = canvas1.ActualWidth;
            dPadHeight = canvas1.ActualHeight;

            bTestIsRunning = false;
            bPaused = false;
            bExitPending = false;

            bLaserLoop = (Boolean)checkBox_laser.IsChecked;
            bHandLoop = (Boolean)checkBox_hand.IsChecked;
            bLMBLoop = (Boolean)checkBox_lmb.IsChecked;

            nLaserWaitTime = Convert.ToInt32(textBox_laser.Text);
            nHandWaitTime = Convert.ToInt32(textBox_hand.Text);
            nLMBWaitTime = Convert.ToInt32(textBox_lmb.Text);

            bMouseMoving = false;

            mousePos = new Point();

            fd = new FaceDetector();

        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            if (!bTestIsRunning)
            {
                udp = new UdpClient();
                ipep = new IPEndPoint(IPAddress.Parse(comboBox_ip.Text), Convert.ToInt32(textBox_port.Text));

                if (tLaser == null)
                {
                    tLaser = new Thread(new ThreadStart(LaserThread));
                    tLaser.Name = "Laser Thread";
                    tLaser.Start();
                }

                if (tHand == null)
                {
                    tHand = new Thread(new ThreadStart(HandThread));
                    tHand.Name = "Hand thread";
                    tHand.Start();
                }

                if (tLMB == null)
                {
                    tLMB = new Thread(new ThreadStart(LMBThread));
                    tLMB.Name = "Left Mouse Button thread";
                    tLMB.Start();
                }

                if (bPaused)
                {
                    bPaused = false;
                }

                bTestIsRunning = true;
                button1.Content = "Running...";
            }
            else
            {
                bPaused = true;
                button1.Content = "Stopped";
                bTestIsRunning = false;
            }
        }

        private void LaserThread()
        {
            byte[] sendBytes = new byte[10];
            while (!bExitPending)
            {
                if (bPaused)
                {
                    Thread.Sleep(1000);
                }
                else
                {
                    if (bLaserLoop)
                    {
                        Random rnd = new Random();
                        //Int32 x = rnd.Next(1, CAM_WIDTH);
                        //Int32 y = rnd.Next(1, CAM_HEIGHT);
                        Int32 x = (Int32)(mousePos.X / dPadWidth * KINECT_WIDTH);
                        Int32 y = (Int32)(mousePos.Y / dPadHeight * KINECT_HEIGHT);

                        Array.Clear(sendBytes, 0, sendBytes.Length);

                        sendBytes[0] = 0x0;
                        sendBytes[1] = Convert.ToByte(true);
                        sendBytes[2] = (byte)x;
                        sendBytes[3] = (byte)(x >> 8);
                        sendBytes[4] = (byte)(x >> 16);
                        sendBytes[5] = (byte)(x >> 24);
                        sendBytes[6] = (byte)y;
                        sendBytes[7] = (byte)(y >> 8);
                        sendBytes[8] = (byte)(y >> 16);
                        sendBytes[9] = (byte)(y >> 24);

                        udp.Send(sendBytes, sendBytes.Length, ipep);
                    }
                    Thread.Sleep(nLaserWaitTime);
                }
            }
        }

        private void HandThread()
        {
            byte[] sendBytes = new byte[10];
            while (!bExitPending)
            {
                if (bPaused)
                {
                    Thread.Sleep(1000);
                }
                else
                {
                    if (bHandLoop)
                    {
                        Array.Clear(sendBytes, 0, sendBytes.Length);

                        if (bMouseMoving)
                        {
                            Int32 x = (Int32)(mousePos.X / dPadWidth * KINECT_WIDTH);
                            Int32 y = (Int32)(mousePos.Y / dPadHeight * KINECT_HEIGHT);

                            sendBytes[0] = 0x1;
                            sendBytes[1] = Convert.ToByte(true);
                            sendBytes[2] = (byte)x;
                            sendBytes[3] = (byte)(x >> 8);
                            sendBytes[4] = (byte)(x >> 16);
                            sendBytes[5] = (byte)(x >> 24);
                            sendBytes[6] = (byte)y;
                            sendBytes[7] = (byte)(y >> 8);
                            sendBytes[8] = (byte)(y >> 16);
                            sendBytes[9] = (byte)(y >> 24);
                        }
                        else
                        {
                            sendBytes[0] = 0x1;
                            sendBytes[1] = Convert.ToByte(false);
                        }

                        udp.Send(sendBytes, sendBytes.Length, ipep);
                    }

                    Thread.Sleep(nHandWaitTime);
                }
            }
        }

        private void LMBThread()
        {
            while (!bExitPending)
            {
                if (bPaused)
                {
                    Thread.Sleep(1000);
                }
                else
                {
                    if (bLMBLoop)
                    {

                    }
                    Thread.Sleep(nLMBWaitTime);
                }
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            bExitPending = true;
        }

        private void canvas1_MouseLeave(object sender, MouseEventArgs e)
        {
            bMouseMoving = false;
        }

        private void canvas1_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            bMouseMoving = true;
        }

        private void canvas1_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            bMouseMoving = false;
        }

        private void canvas1_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            dPadWidth = canvas1.ActualWidth;
            dPadHeight = canvas1.ActualHeight;
        }

        private void canvas1_MouseMove(object sender, MouseEventArgs e)
        {
            mousePos = e.GetPosition(canvas1);
        }

        private void checkBox_hand_Changed(object sender, RoutedEventArgs e)
        {
            bHandLoop = (Boolean)checkBox_hand.IsChecked;
        }

        private void checkBox_laser_Changed(object sender, RoutedEventArgs e)
        {
            bLaserLoop = (Boolean)checkBox_laser.IsChecked;
        }

        private void checkBox_lmb_Changed(object sender, RoutedEventArgs e)
        {
            bLMBLoop = (Boolean)checkBox_lmb.IsChecked;
        }

        private void textBox_hand_TextChanged(object sender, TextChangedEventArgs e)
        {
            nHandWaitTime = Convert.ToInt32(textBox_hand.Text);
        }

        private void textBox_laser_TextChanged(object sender, TextChangedEventArgs e)
        {
            nLaserWaitTime = Convert.ToInt32(textBox_laser.Text);
        }

        private void textBox_lmb_TextChanged(object sender, TextChangedEventArgs e)
        {
            nLMBWaitTime = Convert.ToInt32(textBox_lmb.Text);
        }
    }
}
