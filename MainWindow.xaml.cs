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
using Gauge;
using System.IO.Ports;
using System.IO;
using Microsoft.Win32;

namespace DischargeControlPanel
{
   /// <summary>
   /// Interaction logic for MainWindow.xaml
   /// </summary>
   public partial class MainWindow : Window, IDisposable
   {
      GaugeControl CurrentGauge, AmpHoursGauge, VoltageGauge, PWMDutyGauge;
      SerialManager serialManager;
      StreamWriter FileOutput;


      public MainWindow()
      {
         InitializeComponent();
         InitUI();
         serialManager = new SerialManager();


         string[] portName = SerialPort.GetPortNames();
         foreach(string port in portName)
         {
            MenuItem newMI = new MenuItem()
            {
               Header = port
            };
            newMI.Click += NewMI_Click;
            PortMenu.Items.Add(newMI);
         }
      }

      private void NewMI_Click(object sender, RoutedEventArgs e)
      {
         serialManager.portName = (sender as MenuItem).Header.ToString();
      }

      private void InitUI()
      {
         CurrentGauge = new GaugeControl()
         {
            MinValue = 0,
            MaxValue = 35
         };
         CurrentGauge.SetRegionValue(1, 0, 10, Brushes.DarkRed);
         CurrentGauge.SetRegionValue(2, 10, 20, Brushes.DarkOrange);
         CurrentGauge.SetRegionValue(3, 20, 30, Brushes.DarkGreen);
         CurrentGauge.SetRegionValue(4, 30, 35, Brushes.DarkRed);

         AmpHoursGauge = new GaugeControl()
         {
            MinValue = 0,
            MaxValue = 30
         };
         AmpHoursGauge.SetRegionValue(1, 0, 18, Brushes.DarkRed);
         AmpHoursGauge.SetRegionValue(2, 18, 20, Brushes.DarkOrange);
         AmpHoursGauge.SetRegionValue(3, 20, 24, Brushes.Yellow);
         AmpHoursGauge.SetRegionValue(4, 24, 30, Brushes.DarkGreen);

         VoltageGauge = new GaugeControl()
         {
            MinValue = 0,
            MaxValue = 14
         };
         VoltageGauge.SetRegionValue(1, 0, 10.5, Brushes.DarkRed);
         VoltageGauge.SetRegionValue(2, 10.5, 11, Brushes.Yellow);
         VoltageGauge.SetRegionValue(3, 11, 12, Brushes.DarkGreen);
         VoltageGauge.SetRegionValue(4, 12, 13, Brushes.YellowGreen);
         VoltageGauge.SetRegionValue(5, 13, 14, Brushes.Yellow);

         PWMDutyGauge = new GaugeControl()
         {
            MinValue = 0,
            MaxValue = 100
         };
         PWMDutyGauge.SetRegionValue(1, 0, 60, Brushes.YellowGreen);
         PWMDutyGauge.SetRegionValue(2, 60, 90, Brushes.Green);
         PWMDutyGauge.SetRegionValue(3, 90, 100, Brushes.YellowGreen);



         MainGrid.Children.Add(CurrentGauge);
         Grid.SetColumn(CurrentGauge, 0);
         Grid.SetRow(CurrentGauge, 1);
         MainGrid.Children.Add(VoltageGauge);
         Grid.SetColumn(VoltageGauge, 1);
         Grid.SetRow(VoltageGauge, 1);
         MainGrid.Children.Add(AmpHoursGauge);
         Grid.SetColumn(AmpHoursGauge, 2);
         Grid.SetRow(AmpHoursGauge, 1);
         MainGrid.Children.Add(PWMDutyGauge);
         Grid.SetColumn(PWMDutyGauge, 0);
         Grid.SetRow(PWMDutyGauge, 2);
      }


      private void UpdateUI(ValueSet valueSet)
      {
         CurrentGauge.SetValue(valueSet.Current);
         AmpHoursGauge.SetValue(valueSet.AmpHours);
         VoltageGauge.SetValue(valueSet.Voltage);
         PWMDutyGauge.SetValue(valueSet.PWMDuty);
         timeTextBlock.Text = (valueSet.DischargeDuration / 60000.0).ToString();
      }


      private void DischargeButton_Click(object sender, RoutedEventArgs e)
      {
         if ((bool)DischargeButton.IsChecked)
         {
            string fileName = GetSavePath();
            if (fileName != null)
            {
               if (serialManager.Connect(CurrentSlider.Value))
               {
                  FileOutput = new StreamWriter(fileName);
                  serialManager.arduinoPort.DataReceived += ArduinoPort_DataReceived;
               }
               else
               {
                  MessageBox.Show("Connection Failure..");
                  DischargeButton.IsChecked = false;
               }
            }
         }
         else
         {
            if (serialManager.Disconnect())
            {
               serialManager.arduinoPort.DataReceived -= ArduinoPort_DataReceived;
               if (FileOutput != null)
               {
                  FileOutput.Close();
               }
            }
            else
            {
               MessageBox.Show("Connection Failure..");
               DischargeButton.IsChecked = true;
            }
         }

      }

      private void ArduinoPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
      {
         try
         {
            string data = serialManager.arduinoPort.ReadTo("\n");

            if (FileOutput != null)
            {
               FileOutput.WriteLine(data);
            }

            RTB.AppendText(data);

            ValueSet valueSet = DataParser.GetValues(data);

            UpdateUI(valueSet);

         }
         catch (Exception err)
         {
            MessageBox.Show("Error receiving data: " + err);
         }
      }

      private void SettingsButt_Click(object sender, RoutedEventArgs e)
      {
         MessageBox.Show("What settings do we want?");
      }

      private void CurrentSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
      {
         serialManager.SetCurrentAim(CurrentSlider.Value);
      }



      private string GetSavePath()
      {
         string saveLocation = @"C:\GREENPOWER\DISCHARGE RIG\Discharge Logs";

         SaveFileDialog sfd = new SaveFileDialog();

         if (!Directory.Exists(saveLocation))
         {
            Directory.CreateDirectory(saveLocation);
         }

         sfd.InitialDirectory = saveLocation;
         sfd.Filter = "Comma Separarted Values (*.csv)|*.csv";

         if (sfd.ShowDialog() == true)
         {
            return sfd.FileName;
         }
         return null;
      }

      public void Dispose()
      {
         serialManager.arduinoPort.DataReceived -= ArduinoPort_DataReceived;
         serialManager.Disconnect();
         serialManager.Dispose();
      }
   }
}
