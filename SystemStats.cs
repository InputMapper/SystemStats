using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using ODIF;
using System.Threading;
using System.Management;

namespace SystemStats
{
    [PluginInfo(
    PluginName = "System Stats",
    PluginDescription = "",
    PluginID = 0,
    PluginAuthorName = "InputMapper",
    PluginAuthorEmail = "",
    PluginAuthorURL = "",
    PluginIconPath = @""
)]
    public class SystemStats_Plugin : InputDevicePlugin
    {
        public SystemStats_Plugin()
        {
            this.Devices.Add(new ThisPCStats());
        }
    }

    public class ThisPCStats : InputDevice
    {
        InputChannelTypes.JoyAxis CPU_Ussage, Memory_Ussage, Memory_Total, Memory_Percent, Battery_Percent;
        Thread MonitoringThread;
        public ThisPCStats()
        {
            this.DeviceName = "This PC Stats";
            CPU_Ussage = new InputChannelTypes.JoyAxis("CPU Ussage", "Percentage of total CPU in use") { min_Value = 0, max_Value = 1 };
            Memory_Ussage = new InputChannelTypes.JoyAxis("Memory Used", "Memory used in MB") { min_Value = 0, max_Value = 0 };
            Memory_Percent = new InputChannelTypes.JoyAxis("Memory Percentage", "Percentage of total memory in use") { min_Value = 0, max_Value = 1 };
            Memory_Total = new InputChannelTypes.JoyAxis("Total Memory", "Total system memory in MB") { min_Value = 0, max_Value = 0 };
            Battery_Percent = new InputChannelTypes.JoyAxis("Battery Percentage", "Percentage of battery remaining") { min_Value = 0, max_Value = 1 };

            InputChannels.Add(CPU_Ussage);
            InputChannels.Add(Memory_Ussage);
            InputChannels.Add(Memory_Total);
            InputChannels.Add(Memory_Percent);
            InputChannels.Add(Battery_Percent);

            MonitoringThread = new Thread(monitoringThread);
            MonitoringThread.Start();
        }

        public void monitoringThread()
        {
            EventWaitHandle MyEventWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);

            while (!Global.IsShuttingDown)
            {
                CPU_Ussage.Value = Stats.getCPUUsage();
                Memory_Percent.Value = Stats.getMemoryPercent();
                Memory_Ussage.Value = Stats.getMemoryUssage();
                Memory_Total.Value = Stats.getMemoryTotal();
                Battery_Percent.Value = Stats.getBatteryPercent();
                MyEventWaitHandle.WaitOne(100);
            }
        }

        protected override void Dispose(bool disposing)
        {
            MonitoringThread.Abort();
        }
    }

    internal static class Stats
    {
        static PerformanceCounter pCntr = new PerformanceCounter("Memory", "Available KBytes");
        static ManagementObject processor = new ManagementObject("Win32_PerfFormattedData_PerfOS_Processor.Name='_Total'");

        static System.Windows.Forms.PowerStatus powerStatus = System.Windows.Forms.SystemInformation.PowerStatus;
        private static double _systemMem;

        internal static double getCPUUsage()
        {
            processor.Get();
            return double.Parse(processor.Properties["PercentProcessorTime"].Value.ToString())/100d;
        }

        internal static double getMemoryUssage()
        {
            return (double)pCntr.NextValue();
        }

        internal static double getMemoryPercent()
        {
            double memAvailable, memPhysical;
            memAvailable = getMemoryUssage();
            memPhysical = getMemoryTotal();
            return (memPhysical - memAvailable) / memPhysical;
        }

        internal static double getMemoryTotal()
        {
            if (_systemMem == 0)
                _systemMem = (double)(new Microsoft.VisualBasic.Devices.ComputerInfo().TotalPhysicalMemory)/1024d;
            return _systemMem;
        }

        internal static double getBatteryPercent()
        {
            return powerStatus.BatteryLifePercent;
        }

    }
}
