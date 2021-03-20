using NAudio.CoreAudioApi;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace Bassinator
{
    class SelectDeviceButton : Button
    {
        public MMDevice Device { get; set; }
        public int DeviceID { get; set; }

        public SelectDeviceButton() : base()
        {
            Click += new RoutedEventHandler(SetDevice);
        }

        private void SetDevice(object sender, RoutedEventArgs eventArgs)
        {
            AudioManager.SetDevice(Device);
        }
    }
}
