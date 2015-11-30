using DotRas;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
namespace AutoVPN
{
    class Program
    {
        static System.Threading.ManualResetEvent _quitEvent = new System.Threading.ManualResetEvent(false); 

        static void Main(string[] args)
        {

            VPN vpn = new VPN();
            vpn.Start();
            //PreventShutOff pso = new PreventShutOff();
            //pso.Start();

            _quitEvent.WaitOne();//Pause and listen
        }
        
        public class PreventShutOff
        {
            //public static event PowerModeChangedEventHandler PowerModeChanged;
            [DllImport("user32.dll")]
            static extern bool SetForegroundWindow(IntPtr hWnd);
            System.Timers.Timer timer = new System.Timers.Timer(120000);// Every 2mins(120000) check if bat. low 
            bool runOnce = true;
            private int i;
            public PreventShutOff()
            {
                timer.Elapsed += new System.Timers.ElapsedEventHandler(timer_Elapsed); 
                SystemEvents.PowerModeChanged += new PowerModeChangedEventHandler(SystemEvents_PowerModeChanged);
                this.i = 0;
            }
            private void SystemEvents_PowerModeChanged(object sender, PowerModeChangedEventArgs e)
            { 
                PowerStatus p = SystemInformation.PowerStatus;
                if (p.PowerLineStatus == PowerLineStatus.Online)
                {
                    if (runOnce)
                    {
                        PauseMovie(); 
                        runOnce = false;
                    }
                }
                else
                {
                    runOnce = true;
                    timer.Enabled = true;
                    this.i = 0;
                }
            }
            public void Start()
            {
                timer.Enabled = true;
            }
            private bool BatteryIsLow()
            {
                PowerStatus p = SystemInformation.PowerStatus;
                if (p.PowerLineStatus == PowerLineStatus.Offline)
                {
                    int batterylife = (int)(p.BatteryLifePercent * 100);
                    if (batterylife < 18)//18 Min. bat to pause movie
                    {
                        runOnce = true;
                        return true;
                    }
                }
                else if (p.PowerLineStatus == PowerLineStatus.Online)
                {
                    int batterylife = (int)(p.BatteryLifePercent * 100);
                    if (batterylife == 100 && this.i == 2)//make sure it is fully charged
                    {
                        if (PauseMovie())
                        {
                            System.Threading.Thread.Sleep(2500);
                            PauseMovie();
                        }
                        this.i += 1;//make it run once
                    }
                    else
                        this.i += 1;
                }
                return false;
            }
            private bool PauseMovie()
            { 
                Process[] proc = new Process[10];
                try
                {
                    proc = Process.GetProcessesByName("vlc");
                    IntPtr h = proc[0].MainWindowHandle;
                    SetForegroundWindow(h);
                    SendKeys.SendWait(" "); 
                    return true;
                }
                catch { }
                try
                {
                    proc = Process.GetProcessesByName("popcorn");
                    IntPtr h = proc[0].MainWindowHandle;
                    SetForegroundWindow(h);
                    SendKeys.SendWait(" "); 
                    return true;
                }
                catch { } 
                return false;
            }
            private void timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
            {
                timer.Enabled = false;  
                if (BatteryIsLow())
                {
                    timer.Enabled = false;
                    if (!PauseMovie())
                       MessageBox.Show("The computer is going to die! Plug it in to save it.", "AutoVPN", MessageBoxButtons.OK, MessageBoxIcon.Stop, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly); 
                }
                else
                    timer.Enabled = true; 
            }
        }


        public class VPN
        {
            
            [DllImport("user32.dll")]
            static extern bool SetForegroundWindow(IntPtr hWnd);
            [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
            public static extern IntPtr SetFocus(HandleRef hWnd);
            [DllImport("user32.dll")]
            static extern bool PostMessage(IntPtr hWnd, UInt32 Msg, int wParam, int lParam);
            /*
            [DllImport("user32.dll", SetLastError = true)]
            static extern bool BringWindowToTop(IntPtr hWnd); 
            [DllImport("user32.dll", SetLastError = true)]
            static extern bool BringWindowToTop(HandleRef hWnd);
            [DllImport("user32.dll")]
            static extern bool AttachThreadInput(uint idAttach, uint idAttachTo,
               bool fAttach);
            [DllImport("user32.dll", SetLastError = true)]
            static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId); 
            // When you don't want the ProcessId, use this overload and pass IntPtr.Zero for the second parameter
            [DllImport("user32.dll")]
            static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr ProcessId);
            [DllImport("user32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            static extern bool ShowWindow(IntPtr hWnd, ShowWindowCommands nCmdShow);

            enum ShowWindowCommands
            {
                /// <summary>
                /// Hides the window and activates another window.
                /// </summary>
                Hide = 0,
                /// <summary>
                /// Activates and displays a window. If the window is minimized or 
                /// maximized, the system restores it to its original size and position.
                /// An application should specify this flag when displaying the window 
                /// for the first time.
                /// </summary>
                Normal = 1,
                /// <summary>
                /// Activates the window and displays it as a minimized window.
                /// </summary>
                ShowMinimized = 2,
                /// <summary>
                /// Maximizes the specified window.
                /// </summary>
                Maximize = 3, // is this the right value?
                /// <summary>
                /// Activates the window and displays it as a maximized window.
                /// </summary>       
                ShowMaximized = 3,
                /// <summary>
                /// Displays a window in its most recent size and position. This value 
                /// is similar to <see cref="Win32.ShowWindowCommand.Normal"/>, except 
                /// the window is not activated.
                /// </summary>
                ShowNoActivate = 4,
                /// <summary>
                /// Activates the window and displays it in its current size and position. 
                /// </summary>
                Show = 5,
                /// <summary>
                /// Minimizes the specified window and activates the next top-level 
                /// window in the Z order.
                /// </summary>
                Minimize = 6,
                /// <summary>
                /// Displays the window as a minimized window. This value is similar to
                /// <see cref="Win32.ShowWindowCommand.ShowMinimized"/>, except the 
                /// window is not activated.
                /// </summary>
                ShowMinNoActive = 7,
                /// <summary>
                /// Displays the window in its current size and position. This value is 
                /// similar to <see cref="Win32.ShowWindowCommand.Show"/>, except the 
                /// window is not activated.
                /// </summary>
                ShowNA = 8,
                /// <summary>
                /// Activates and displays the window. If the window is minimized or 
                /// maximized, the system restores it to its original size and position. 
                /// An application should specify this flag when restoring a minimized window.
                /// </summary>
                Restore = 9,
                /// <summary>
                /// Sets the show state based on the SW_* value specified in the 
                /// STARTUPINFO structure passed to the CreateProcess function by the 
                /// program that started the application.
                /// </summary>
                ShowDefault = 10,
                /// <summary>
                ///  <b>Windows 2000/XP:</b> Minimizes a window, even if the thread 
                /// that owns the window is not responding. This flag should only be 
                /// used when minimizing windows from a different thread.
                /// </summary>
                ForceMinimize = 11
            }
            
            [DllImport("user32.dll", SetLastError = true)]
            static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

            //[DllImport("user32.dll")]
            //private static extern IntPtr GetForegroundWindow();

            [DllImport("user32.dll", SetLastError = true)]
            static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

            // When you don't want the ProcessId, use this overload and pass IntPtr.Zero for the second parameter
            [DllImport("user32.dll")]
            static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr ProcessId);
            [DllImport("user32.dll")]
            static extern bool AttachThreadInput(uint idAttach, uint idAttachTo,
               bool fAttach);
            */
             
            System.Timers.Timer timer = new System.Timers.Timer(30000);
            
            public VPN()
            {
                //Cause isnetworkavailable wont fire if internet is still on and just drop VPN 
                NetworkChange.NetworkAddressChanged += new NetworkAddressChangedEventHandler(AddressChangedCallback); 
                timer.Elapsed += new System.Timers.ElapsedEventHandler(timer_Elapsed);
                this.isConnecting = false;
            } 
            //Public Methods
            public void Start()
            {  
                if (!isConnected())
                    Connect();    
            }
            void timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
            {
                timer.Enabled = false;
                if (!isConnected())
                    Connect(); 
                else 
                    timer.Enabled = true;
            }
            void AddressChangedCallback(object sender, EventArgs e)
            {
                if (!isConnected())
                    Connect();
            }
            /*
            public static void forceSetForegroundWindow(IntPtr hWnd, IntPtr mainThreadId)
            {
                IntPtr foregroundThreadID;// = GetWindowThreadProcessId(GetForegroundWindow(), IntPtr.Zero);
                if (foregroundThreadID != mainThreadId)
                {
                    AttachThreadInput(mainThreadId, foregroundThreadID, true);
                    SetForegroundWindow(hWnd);
                    AttachThreadInput(mainThreadId, foregroundThreadID, false);
                }
                else
                    SetForegroundWindow(hWnd);
            }
            
            private static void ForceForegroundWindow(IntPtr hWnd)
            {

                uint foreThread = GetWindowThreadProcessId(GetForegroundWindow(), IntPtr.Zero);

                uint appThread = GetCurrentThreadId();

                const uint SW_SHOW = 5;

                if (foreThread != appThread)
                {

                    AttachThreadInput(foreThread, appThread, true);

                    BringWindowToTop(hWnd);

                    ShowWindow(hWnd, SW_SHOW);

                    AttachThreadInput(foreThread, appThread, false);

                }

                else
                {

                    BringWindowToTop(hWnd);

                    ShowWindow(hWnd, SW_SHOW);

                }

            }
             * */
            public void Connect()
            {
                if (NetworkInterface.GetIsNetworkAvailable() && !isConnected() && !this.isConnecting)
                {
                    this.isConnecting = true;
                    using (RasPhoneBook pb = new RasPhoneBook())
                    {
                        pb.Open();
                        RasEntryCollection entries = pb.Entries; 
                        RasDialer rd = new RasDialer();  
                        rd.EntryName = "US TX";
                        rd.PhoneBookPath = pb.Path;
                        rd.Credentials = new NetworkCredential("x2934389", "JooWB3teMg");
                        System.Threading.Thread.Sleep(20000);
                        while (!isConnected())
                        {
                            if (!rd.IsBusy)
                            {
                                try
                                {
                                    rd.Dial();
                                    System.Threading.Thread.Sleep(20000); //increase time if still seeing warning about already connecting
                                }
                                catch (Exception ex)
                                {
                                    if (ex.Message.IndexOf("A connection to the remote computer could not be established.") != -1)
                                    { ;}
                                    //else
                                        //CloseWarning();
                                }
                            }
                        } 
                        this.isConnecting = false;
                        timer.Enabled = true;
                        
                        /* Manually add L2TP with preshared key
                        string l2tpConName = "US-TX";
                        string ip = "";
                        string username = "x2934389";
                        string password = "JooWB3teMg";
                        string sharedKey = "mysafety";
                        //System.Diagnostics.Process.Start("rasdial.exe", "VPN US-TX x2934389 JooWB3teMg"); No work :( need L2TP
                        RasEntry entryL2TP = RasEntry.CreateVpnEntry(l2tpConName, ip, RasVpnStrategy.L2tpOnly, RasDevice.GetDeviceByName("(L2TP)", RasDeviceType.Vpn));

                        pb.Entries.Add(entryL2TP);

                        entryL2TP.UpdateCredentials(new NetworkCredential(username, password));
                        entryL2TP.Update();
                        entryL2TP.Options.UsePreSharedKey = true;
                        entryL2TP.UpdateCredentials(RasPreSharedKey.Client, sharedKey);
                        entryL2TP.Update();*/
                    }
                }
                else
                    Connect();
            }
            private void CloseWarning()
            {
                Process[] proc = new Process[10];
                proc = Process.GetProcessesByName("rasautou");
                if (proc.Length > 0)
                {
                    IntPtr h = proc[0].MainWindowHandle;
                    //ALT+TAB below
                    uint WM_SYSCOMMAND = 0x0112;
                    int SC_PREVWINDOW = 0xF050;
                    PostMessage(proc[0].MainWindowHandle, WM_SYSCOMMAND, SC_PREVWINDOW, 0);
                    SetForegroundWindow(h);
                    SetFocus(new HandleRef(null, proc[0].MainWindowHandle));
                    SendKeys.SendWait(" ");
                }
            }
            public bool isConnected()
            {
                foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (nic.Name == "VPN US-TX") 
                        return true; 
                }
                return false;
            }

            protected bool isConnecting { get; private set; }
        }
    }
}
