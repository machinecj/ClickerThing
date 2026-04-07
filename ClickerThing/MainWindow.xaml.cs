using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Timers;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Drawing;
using System;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ClickerThing
{
    
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private System.Timers.Timer _intervalTimer;
        private bool _isRangeActive;
        private bool _isClickingActive;
        private double mouseX;
        private double mouseY;

        public MainWindow()
        {
            DataContext = this;
            Interval = 0.1;
            _intervalTimer = new System.Timers.Timer(Interval * 1000);
            _intervalTimer.Elapsed += OnIntervalTimerEvent;
            _intervalTimer.AutoReset = true;
            _intervalTimer.Enabled = _isClickingActive;
            InitializeComponent();
        }

        private double interval;

        public double Interval
        {
            get { return interval; }
            set
            {
                interval = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Interval"));
            }
        }

        private double range;

        public double Range
        {
            get { return range; }
            set { 
                range = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Range"));
            }
        }

        private double delay;

        public double Delay
        {
            get { return delay; }
            set { 
                delay = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Delay"));
            }
        }

        private double duration;

        public double Duration
        {
            get { return duration; }
            set { 
                duration = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Duration"));
            }
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetCursorPos(out POINT cursPoint);

        public struct POINT
        {
            public int X;
            public int Y;

            public POINT(int x, int y)
            {
                this.X = x;
                this.Y = y;
            }
        }

        internal enum INPUT_TYPE : uint
        {
            INPUT_MOUSE = 0,
            INPUT_KEYBOARD = 1,
            INPUT_HARDWARE = 2
        }

        public struct MOUSEINPUT
        {
            public int dX;
            public int dY;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        public struct KEYBDINPUT
        {
            public ushort wVk; // try with uint? WORD vs DWORD? DWORD can be a UINT, so is WOPRD specifically USHORT
            public ushort wScan; //
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        public struct HARDWAREINPUT
        {
            public uint uMSG; //
            public ushort wParamL; //
            public ushort wParamH;
        }

        public struct INPUT
        {
            public uint type;
            public MOUSEINPUT mi;
            public KEYBDINPUT ki;
            public HARDWAREINPUT hi;
        }







        [DllImport("user32.dll")]
        static extern IntPtr GetDC(IntPtr hWnd); // get device context

        [DllImport("gdi32.dll")]
        static extern int GetDeviceCaps(IntPtr hDC, int nIndex); // 

        [DllImport("user32.dll")]
        static extern bool ReleaseDC(IntPtr hWnd, IntPtr hDC); // release device context

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, uint dwExtraInfo); // docs say this is old, and to use SendInput() instead

        [DllImport("user32.dll")]
        public static extern uint SendInput(uint cInputs, [MarshalAs(UnmanagedType.LPArray), In] INPUT[] pInputs, int cbSize);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern uint GetLastError();

        private POINT ConvertPixelCoordsToWPFUnits(int x, int y)
        {
            IntPtr dDC = GetDC(IntPtr.Zero); // get entire screen's device context
            int dpi = GetDeviceCaps(dDC, 88); // get screen dpi
            bool rv = ReleaseDC(IntPtr.Zero, dDC);

            double wpfPhysicalUnitSize = (1d / 96d) * (double)dpi;
            POINT wpfUnits = new POINT((int)(wpfPhysicalUnitSize * (double)x), (int)(wpfPhysicalUnitSize * (double)y));

            return wpfUnits;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void IntervalTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {

        }

        private void OnIntervalTimerEvent(object source, ElapsedEventArgs e)
        {
            //POINT pt;
            //Debug.WriteLine("wooop {0:HH:mm:ss.fff} - {1}", e.SignalTime, GetCursorPos( out pt ) );
            //pt = ConvertPixelCoordsToWPFUnits(pt.X, pt.Y);
            //Debug.WriteLine(pt.X + " - " + pt.Y);
            UpdateMouseCoords();

            INPUT[] inputs = new INPUT[1];

            inputs[0].type = 0;
            inputs[0].mi.dX = (int)mouseX;
            inputs[0].mi.dY = (int)mouseY;
            inputs[0].mi.dwFlags = 0x0002;

            int byteCount = Marshal.SizeOf(typeof(INPUT));
            /*int size;
            unsafe
            {
                size = sizeof(INPUT);
            }*/

            //int byteCount = Buffer.ByteLength(inputs);
            //uint uSent = SendInput((uint)1, inputs, 5);
            mouse_event(0x0002, (uint)mouseX, (uint)mouseY, 0, 0); // this works just fine... 
            mouse_event(0x0004, (uint)mouseX, (uint)mouseY, 0, 0);

            int error = Marshal.GetLastWin32Error();
            uint error2 = GetLastError();

            //Debug.WriteLine(mouseX + " " + mouseY + " " + byteCount + " " + uSent + " " + error + " " + error2);
        }

        private void UpdateMouseCoords()
        {
            GetCursorPos(out POINT pt);
            pt = ConvertPixelCoordsToWPFUnits(pt.X, pt.Y);
            mouseX = pt.X;
            mouseY = pt.Y;
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isClickingActive)
            {
                _isClickingActive = false;
                _intervalTimer.Stop();
                StartButton.Content = "Start";
                
            }
            else
            {
                _isClickingActive = true;
                _intervalTimer.Interval = Interval * 1000;
                _intervalTimer.Start();
                StartButton.Content = "Stop";
            }
        }
    }
}