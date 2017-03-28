using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System;

public class MonitorHelper : MonoBehaviour {

    delegate bool EnumMonitorsDelegate(IntPtr hMonitor, IntPtr hdcMonitor, ref Rect lprcMonitor, IntPtr dwData);

    [System.Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct Rect
    {
        public int left;
        public int top;
        public int right;
        public int bottom;

        public override string ToString()
        {
            return "left: " + left + ", top: " + top + ", right: " + right + ", bottom: " + bottom + ", width: " + (right - left) + " , height: " + (bottom - top);
        }
    }

    /// <summary>
    /// The struct that contains the display information
    /// </summary>
    [System.Serializable]
    public class DisplayInfo
    {
        public string Availability;
        public string ScreenHeight;
        public string ScreenWidth;

        public Rect MonitorArea;
        public Rect WorkArea;

        public DEVMODE devMode;
        public DISPLAY_DEVICE displayDevice;

        public Rect PhysicalArea;
        public float scaleFactor;

        public void Process()
        {
            PhysicalArea = new Rect();

            var width = devMode.dmPelsWidth;
            var height = devMode.dmPelsHeight;
            var x = devMode.dmPositionX;
            var y = devMode.dmPositionY;

            PhysicalArea.left = x;
            PhysicalArea.top = y;
            PhysicalArea.right = x + width;
            PhysicalArea.bottom = y + height;

            scaleFactor = width / (float) (MonitorArea.right - MonitorArea.left);

            // unity is only main screen dpi aware.
            // that means: everything is in the scale of the main screen.
            // left top point is always correct (?) - width and height have to be scaled.
        }
    }

    /// <summary>
    /// Collection of display information
    /// </summary>
    [System.Serializable]
    public class DisplayInfoCollection : List<DisplayInfo>
    {
    }
    
    public List<DisplayInfo> displays;

    void Start()
    {
        // for debugging purposes
        displays = GetDisplays();
        AddAdditionalInfos(displays);

        foreach (var dv in displays)
        {
            print("found a screen");
            print(dv.ScreenWidth + "x" + dv.ScreenHeight);
            print("monitor area: " + dv.MonitorArea);
        }
    }

    public static void AddAdditionalInfos(List<DisplayInfo> displayInfo)
    {
        for(int id = 0; id < displayInfo.Count; id++)
        { 
            DEVMODE vDevMode = new DEVMODE();
            DISPLAY_DEVICE d = new DISPLAY_DEVICE();

            d.cb = Marshal.SizeOf(d);
            try
            {
                // for (uint id = 0; EnumDisplayDevices(null, id, ref d, 0); id++)
                EnumDisplayDevices(null, (uint) id, ref d, 0);
                
                Debug.Log(
                    String.Format("{0}, {1}, {2}, {3}, {4}, {5}",
                                id,
                                d.DeviceName,
                                d.DeviceString,
                                d.StateFlags,
                                d.DeviceID,
                                d.DeviceKey
                                )
                                );
                d.cb = Marshal.SizeOf(d);

                if ((d.StateFlags & DisplayDeviceStateFlags.AttachedToDesktop) == DisplayDeviceStateFlags.AttachedToDesktop)
                {
                    EnumDisplaySettings(d.DeviceName, -1, ref vDevMode);
                    // devmodes.Add(vDevMode);
                    displayInfo[id].devMode = vDevMode;
                }

                displayInfo[id].displayDevice = d;
                displayInfo[id].Process();
            }
            catch (Exception ex)
            {
                Console.WriteLine(String.Format("{0}", ex.ToString()));
            }
        }
    }

    /// <summary>
    /// Returns the number of Displays using the Win32 functions
    /// </summary>
    /// <returns>collection of Display Info</returns>
    public static DisplayInfoCollection GetDisplays()
    {
        DisplayInfoCollection col = new DisplayInfoCollection();

        EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero,
            delegate (IntPtr hMonitor, IntPtr hdcMonitor, ref Rect lprcMonitor, IntPtr dwData)
            {
                MonitorInfo mi = new MonitorInfo();
                mi.size = Marshal.SizeOf(mi);
                bool success = GetMonitorInfo(hMonitor, ref mi);
                if (success)
                {
                    DisplayInfo di = new DisplayInfo();
                    di.ScreenWidth = (mi.monitor.right - mi.monitor.left).ToString();
                    di.ScreenHeight = (mi.monitor.bottom - mi.monitor.top).ToString();
                    di.MonitorArea = mi.monitor;
                    di.WorkArea = mi.work;
                    di.Availability = mi.flags.ToString();
                    col.Add(di);
                }
                return true;
            }, IntPtr.Zero);
        return col;
    }

    public static DisplayInfo GetDisplay(int index)
    {
        var displays = GetDisplays();
        AddAdditionalInfos(displays);

        if(displays != null && index >= 0 && index < displays.Count)
        {
            return displays[index];
        }

        // set up a default screen
        var di = new DisplayInfo();
        var monitorArea = new Rect();
        monitorArea.left = Screen.currentResolution.width * index;
        monitorArea.right = Screen.currentResolution.width * (index + 1);
        monitorArea.top = 0;
        monitorArea.bottom = Screen.currentResolution.height;
        di.MonitorArea = monitorArea;
        di.WorkArea = monitorArea;
        return di;
    }
    
    [DllImport("user32.dll")]
    static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip,
       EnumMonitorsDelegate lpfnEnum, IntPtr dwData);

    [StructLayout(LayoutKind.Sequential)]
    public struct MonitorInfo
    {
        public int size;
        public Rect monitor;
        public Rect work;
        public uint flags;
    }

    [DllImport("user32.dll")]
    static extern bool GetMonitorInfo(IntPtr hMonitor, ref MonitorInfo lpmi);
    
    // display settings for scale

    [DllImport("user32.dll")]
    public static extern bool EnumDisplaySettings(string deviceName, int modeNum, ref DEVMODE devMode);
    
    [System.Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct DEVMODE
    {
        private const int CCHDEVICENAME = 0x20;
        private const int CCHFORMNAME = 0x20;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x20)]
        public string dmDeviceName;
        public short dmSpecVersion;
        public short dmDriverVersion;
        public short dmSize;
        public short dmDriverExtra;
        public int dmFields;
        public int dmPositionX;
        public int dmPositionY;
        public ScreenOrientation dmDisplayOrientation;
        public int dmDisplayFixedOutput;
        public short dmColor;
        public short dmDuplex;
        public short dmYResolution;
        public short dmTTOption;
        public short dmCollate;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x20)]
        public string dmFormName;
        public short dmLogPixels;
        public int dmBitsPerPel;
        public int dmPelsWidth;
        public int dmPelsHeight;
        public int dmDisplayFlags;
        public int dmDisplayFrequency;
        public int dmICMMethod;
        public int dmICMIntent;
        public int dmMediaType;
        public int dmDitherType;
        public int dmReserved1;
        public int dmReserved2;
        public int dmPanningWidth;
        public int dmPanningHeight;
    }

    [DllImport("user32.dll")]
    static extern bool EnumDisplayDevices(string lpDevice, uint iDevNum, ref DISPLAY_DEVICE lpDisplayDevice, uint dwFlags);

    [System.Serializable]
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct DISPLAY_DEVICE
    {
        [MarshalAs(UnmanagedType.U4)]
        public int cb;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string DeviceName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceString;
        [MarshalAs(UnmanagedType.U4)]
        public DisplayDeviceStateFlags StateFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceID;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceKey;
    }

    [Flags()]
    public enum DisplayDeviceStateFlags : int
    {
        /// <summary>The device is part of the desktop.</summary>
        AttachedToDesktop = 0x1,
        MultiDriver = 0x2,
        /// <summary>The device is part of the desktop.</summary>
        PrimaryDevice = 0x4,
        /// <summary>Represents a pseudo device used to mirror application drawing for remoting or other purposes.</summary>
        MirroringDriver = 0x8,
        /// <summary>The device is VGA compatible.</summary>
        VGACompatible = 0x10,
        /// <summary>The device is removable; it cannot be the primary display.</summary>
        Removable = 0x20,
        /// <summary>The device has more display modes than its output devices support.</summary>
        ModesPruned = 0x8000000,
        Remote = 0x4000000,
        Disconnect = 0x2000000
    }
}