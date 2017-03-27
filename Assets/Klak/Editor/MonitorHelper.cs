using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System;

public class MonitorHelper : MonoBehaviour {

    delegate bool EnumMonitorsDelegate(IntPtr hMonitor, IntPtr hdcMonitor, ref Rect lprcMonitor, IntPtr dwData);

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
    public class DisplayInfo
    {
        public string Availability { get; set; }
        public string ScreenHeight { get; set; }
        public string ScreenWidth { get; set; }
        public Rect MonitorArea { get; set; }
        public Rect WorkArea { get; set; }
    }

    /// <summary>
    /// Collection of display information
    /// </summary>
    public class DisplayInfoCollection : List<DisplayInfo>
    {
    }
    
    void Start()
    {
        // for debugging purposes
        var ds = GetDisplays();
        foreach(var d in ds)
        {
            print("found a screen");
            print(d.ScreenWidth + "x" + d.ScreenHeight);
            print("monitor area: " + d.MonitorArea);
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
}
