using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
// MC600需要用到的命名空间
using ZOLIXMC6002Lib;
using AxZOLIXMC6002Lib;
// AForge控制相机需用到的命名空间
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using System.Drawing;
using System.Drawing.Imaging;
using AForge;
using AForge.Video;
using AForge.Video.DirectShow;
using AForge.Imaging;
using AForge.Imaging.Filters;


namespace MC600_Camera_Ctrl_2
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        //MC600实例化以及参数预设
        AxZOLIXMC6002 axMC600 = new AxZOLIXMC6002();
        short nAxis; //0->X; 1->Y(Default); 2->Z; 3->T
        int MoveAmount = 0;

        //相机1的实例化以及参数预设
        public string saveName1 = "Cam_test1";
        public string saveDir1 = "D:";
        private FilterInfoCollection videoDevices1;
        private VideoCaptureDevice captureDevice1;
        private PictureBox picture1;
        private Bitmap camTemp1; 

        //相机2的实例化以及参数预设
        public string saveName2 = "Cam_test2";
        public string saveDir2 = "D:";
        private FilterInfoCollection videoDevices2;
        private VideoCaptureDevice captureDevice2;
        private PictureBox picture2;
        private Bitmap camTemp2; 

        //  窗口初始化函数
        // 窗口初始化函数
        public MainWindow()
        {
            InitializeComponent();
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)  // Plugin the USB First before openning the soft
        {
            //MC600的初始化
            System.Windows.Forms.Integration.WindowsFormsHost host = new System.Windows.Forms.Integration.WindowsFormsHost();
            axMC600.BeginInit();
            host.Child = axMC600;
            dockPanel1.Children.Add(host);
            axMC600.EndInit();

            UpdatePortList();
            refreshConnStatus();

            MoveAmountText.Text = MoveAmount.ToString();
            stepAngle.Text = "0";
            stagePitch.Text = "0";
            microStep.Text = "0";
            driverRate.Text = "0";
            radius.Text = "0";
            currentPos.Text = "0";

            //相机1的初始化
            picture1 = this.pictureHost1.Child as System.Windows.Forms.PictureBox;
            updatePortListCam1();
            saveDirT1.Text = saveDir1;
            saveNameT1.Text = saveName1;


            //相机2的初始化
            picture2 = this.pictureHost2.Child as System.Windows.Forms.PictureBox;
            updatePortListCam2();
            saveDirT2.Text = saveDir2;
            saveNameT2.Text = saveName2;


        }

        // MC600控制相关函数       
        private void Button_Click_Close(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Button_MouseRightButtonDown_1(object sender, MouseButtonEventArgs e)
        {
            //MessageBox.Show("This window is for testing");
            short workstate = 1, state = 1, dir = 1;
            nAxis = 1;
            int x;
            x = 10;
            workstate = axMC600.Getworkstate(nAxis);
            axMC600.GoPosition(nAxis, workstate, state, dir, x);
        }

        private void refreshConnStatus() //Done
        {
            btnDisconn.IsEnabled = axMC600.GetIsOpen();
            btnScan.IsEnabled = !axMC600.GetIsOpen();
        }

        private void btnScan_Click_1(object sender, RoutedEventArgs e)  //连接
        {
            if (comboCom.Items.Count == 0)
            {
                System.Windows.MessageBox.Show("未找到设备。请确认检测系统硬件已经连接到电脑且电源已打开", "未找到设备", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            else
            {
                try
                {
                    axMC600.USBSerials = comboCom.SelectedItem.ToString();
                    if (axMC600.Connect())
                    {
                        System.Windows.MessageBox.Show("连接成功");
                        //OnRadio1();
                    }
                    else
                    {
                        System.Windows.MessageBox.Show("连接失败");
                    }
                }
                catch
                {
                    System.Windows.MessageBox.Show("设备连接异常。请确认检测系统硬件已经连接到电脑且电源已打开", "设备连接失败", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            refreshConnStatus();
            updateParam();

            RadioY.IsChecked = true;
            nAxis = 1;
            radioMm.IsChecked = true;
            axMC600.SetUnit(nAxis, 1);


        }    // Connect the comPort

        private void btnDisconn_Click_1(object sender, RoutedEventArgs e)// Done
        {
            try
            {
                axMC600.DisConnect();
               System.Windows.MessageBox.Show("连接已断开", "断开成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch
            {
                System.Windows.MessageBox.Show("设备未连接，不用关闭", "设备未连接", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            refreshConnStatus();
        }

        private void UpdatePortList() //Done
        {
            comboCom.Items.Clear();
            int i;
            string str;
            axMC600.USBMODE = true;
            int nCount;
            nCount = axMC600.SearchZolixUSBDevice();
            for (i = 0; i < nCount; i++)
            {
                str = axMC600.GetZolixUSBSerial(i);
                comboCom.Items.Add(str);
            }
        }

        private void Refresh_Click_1(object sender, RoutedEventArgs e)
        {
            UpdatePortList();
        }

        private void RadioX_Click_1(object sender, RoutedEventArgs e)
        {
            nAxis = 0;
            radioMm.IsChecked = true;
            axMC600.SetUnit(nAxis, 1);
            updateParam();
            //MessageBox.Show("Axis: X");
        }

        private void RadioY_Click_1(object sender, RoutedEventArgs e)
        {
            nAxis = 1;
            radioMm.IsChecked = true;
            axMC600.SetUnit(nAxis, 1);
            updateParam();
            // MessageBox.Show("Axis: Y");
        }

        private void MoveForward_Click_1(object sender, RoutedEventArgs e)
        {
            // workstate为1代表开环，state为1代表相对位移，dir为1代表正向
            // workstate为0代表闭环，state为0代表绝对位移，dir为0代表负向
            short workstate;
            short state = 1, dir = 1;
            MoveAmount = int.Parse(MoveAmountText.Text);
            workstate = axMC600.Getworkstate(nAxis);
            axMC600.GoPosition(nAxis, workstate, state, dir, MoveAmount);
            updateParam();
        }

        private void MoveBackward_Click_1(object sender, RoutedEventArgs e)
        {
            short workstate;
            short state = 1, dir = 0;
            MoveAmount = int.Parse(MoveAmountText.Text);
            workstate = axMC600.Getworkstate(nAxis);
            axMC600.GoPosition(nAxis, workstate, state, dir, MoveAmount);
            updateParam();
        }

        private void GoMechHome_Click_1(object sender, RoutedEventArgs e)
        {
            axMC600.GoHome(nAxis);
            updateParam();
        }

        private void Stop_Click_1(object sender, RoutedEventArgs e)
        {
            axMC600.Stop(nAxis);
            updateParam();
        }

        private void readParam_Click_1(object sender, RoutedEventArgs e)
        {
            updateParam();
        }

        private void updateParam()
        {
            stepAngle.Text = axMC600.GetStepAngle(nAxis).ToString();
            stagePitch.Text = axMC600.GetPitch(nAxis).ToString();
            microStep.Text = axMC600.GetMicroStep(nAxis).ToString();
            driverRate.Text = axMC600.GetDriveRat(nAxis).ToString();
            radius.Text = axMC600.Getradius(nAxis).ToString();
            currentPos.Text = axMC600.GetPosition(nAxis).ToString();
        }

        private void radioMm_Click_1(object sender, RoutedEventArgs e) // Set the Unit: 0->deg; 1->mm; 2->step; 3->um
        {
            axMC600.SetUnit(nAxis, 1);
            updateParam();
        }

        private void radioStep_Click_1(object sender, RoutedEventArgs e)
        {
            axMC600.SetUnit(nAxis, 2);
            updateParam();
        }

        private void radioUm_Click_1(object sender, RoutedEventArgs e)
        {
            axMC600.SetUnit(nAxis, 3);
            updateParam();
        }


        // 相机1的控制相关函数
        private void connectCam1_Click_1(object sender, RoutedEventArgs e)
        {
            if (comboCam1.Items.Count == 0)
            {
                System.Windows.MessageBox.Show("未找到设备。请确认检测系统硬件已经连接到电脑且电源已打开", "未找到设备", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            else
            {
                captureDevice1 = new VideoCaptureDevice(videoDevices1[comboCam1.SelectedIndex].MonikerString);
                captureDevice1.NewFrame += new NewFrameEventHandler(captureDevice1_NewFrame);
                captureDevice1.DesiredFrameRate = 10;
                captureDevice1.Start(); 
            }
        }

        private void captureDevice1_NewFrame(object sender, NewFrameEventArgs eventArgs) //帧处理程序
        {
            Bitmap bitmap = (Bitmap)eventArgs.Frame.Clone();
            camTemp1 = bitmap;
            
            //this.picture1.Invoke(new MethodInvoker(delegate(){ picture1.Image = bitmap; }));
            picture1.Image = bitmap;
        }

        private void capturePic1_Click_1(object sender, RoutedEventArgs e)
        {
            string filename = saveDir1 + "/" + saveName1 + ".jpg";
            // 设置图像质量：1-100
            long[] quality = new long[1];
            quality[0] = 100;
            EncoderParameter p = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);
            EncoderParameters ps = new EncoderParameters(1);
            ps.Param[0] = p;
            camTemp1.Save(filename, GetCodecInfo("image/jpeg"), ps);
            // 直接存储 Temp.Save(@"D:\test2.jpeg",System.Drawing.Imaging.ImageFormat.Jpeg)
        }

        private void updatePortListCam1()
        {
            comboCam1.Items.Clear();
            try
            {
                // 枚举所有视频输入设备
                videoDevices1 = new FilterInfoCollection(FilterCategory.VideoInputDevice);
                if (videoDevices1.Count == 0)
                    throw new ApplicationException();
                foreach (FilterInfo device in videoDevices1)
                {
                    comboCam1.Items.Add(device.Name);
                }
                //预先选中第一项  comboCam1.SelectedIndex = 0;
            }
            catch (ApplicationException)
            {
                comboCam1.Items.Add("No local capture devices");
                videoDevices1 = null;
            }
        }

        private void refreshCamList1_Click_1(object sender, RoutedEventArgs e)
        {
            updatePortListCam1();
        }

        private void Exit1_Click_1(object sender, RoutedEventArgs e)
        {
            if (captureDevice1 != null)
            {
                if (captureDevice1.IsRunning)
                {
                    captureDevice1.SignalToStop();
                }
                captureDevice1 = null;
                picture1.Image = null;
            }
        }

        private void changeSaveName1_Click_1(object sender, RoutedEventArgs e)
        {
            saveName1 = saveNameT1.Text;
        }

        private void selSaveDir1_Click_1(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog();
            fbd.Description = "请选择图片存储的文件夹";
            fbd.ShowNewFolderButton = true;
            //fbd.RootFolder = Environment.SpecialFolder.Personal;
            System.Windows.Forms.DialogResult result = fbd.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                string folderName = fbd.SelectedPath;
                if (folderName != "")
                {
                    saveDir1 = folderName;
                    saveDirT1.Text = saveDir1;
                }
            }
        }


        //相机2的控制相关函数
        private void connectCam2_Click_1(object sender, RoutedEventArgs e)
        {
            if (comboCam2.Items.Count == 0)
            {
                System.Windows.MessageBox.Show("未找到设备。请确认检测系统硬件已经连接到电脑且电源已打开", "未找到设备", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            else
            {
                captureDevice2 = new VideoCaptureDevice(videoDevices2[comboCam2.SelectedIndex].MonikerString);
                captureDevice2.NewFrame += new NewFrameEventHandler(captureDevice2_NewFrame);
                captureDevice2.DesiredFrameRate = 10;
                captureDevice2.Start();  
            }
        }

        private void captureDevice2_NewFrame(object sender, NewFrameEventArgs eventArgs)//帧处理程序
        {
            Bitmap bitmap = (Bitmap)eventArgs.Frame.Clone();
            camTemp2 = bitmap;
            picture2.Image = bitmap;
        }

        private void capturePic2_Click_1(object sender, RoutedEventArgs e)
        {           
            string filename = saveDir2 + "/" + saveName2 + ".jpg";
            // 设置图像质量：1-100
            long[] quality = new long[1];
            quality[0] = 100;
            EncoderParameter p = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);
            EncoderParameters ps = new EncoderParameters(1);
            ps.Param[0] = p;
            camTemp2.Save(filename, GetCodecInfo("image/jpeg"), ps);
            // 直接存储 Temp.Save(@"D:\test2.jpeg",System.Drawing.Imaging.ImageFormat.Jpeg)
        }

        private void updatePortListCam2()
        {
            comboCam2.Items.Clear();
            try
            {
                // 枚举所有视频输入设备
                videoDevices2 = new FilterInfoCollection(FilterCategory.VideoInputDevice);
                if (videoDevices2.Count == 0)
                    throw new ApplicationException();
                foreach (FilterInfo device in videoDevices2)
                {
                    comboCam2.Items.Add(device.Name);
                }
                //预先选中第一项 comboCam1.SelectedIndex = 0;
            }
            catch (ApplicationException)
            {
                comboCam2.Items.Add("No local capture devices");
                videoDevices2 = null;
            }
        }

        private void refreshCamList2_Click_1(object sender, RoutedEventArgs e)
        {
            updatePortListCam2();
        }

        private void Exit2_Click_1(object sender, RoutedEventArgs e)
        {
            if (captureDevice2 != null)
            {
                if (captureDevice2.IsRunning)
                {
                    captureDevice2.SignalToStop();
                }
                captureDevice2 = null;
                picture2.Image = null;
            }
        }

        private void changeSaveName2_Click_1(object sender, RoutedEventArgs e)
        {
            saveName2 = saveNameT2.Text;
        }

        private void selSaveDir2_Click_1(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog();
            fbd.Description = "请选择图片存储的文件夹";
            fbd.ShowNewFolderButton = true;
            //fbd.RootFolder = Environment.SpecialFolder.Personal;
            System.Windows.Forms.DialogResult result = fbd.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                string folderName = fbd.SelectedPath;
                if (folderName != "")
                {
                    saveDir2 = folderName;
                    saveDirT2.Text = saveDir2;
                }
            }
        }

        // 相机1、2通用函数，用于存储图像的格式选择
        private static ImageCodecInfo GetCodecInfo(string mimeType)
        {
            ImageCodecInfo[] CodecInfo = ImageCodecInfo.GetImageEncoders();
            foreach (ImageCodecInfo ici in CodecInfo)
            {
                if (ici.MimeType == mimeType) return ici;
            }
            return null;
        }
    }
}
