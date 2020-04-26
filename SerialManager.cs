using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace DischargeControlPanel
{
  public class SerialManager
   {
      public SerialPort arduinoPort;
      public string portName = "COM12";

      public  SerialManager()
      {
         arduinoPort = new SerialPort();
      }

      public bool Connect(double currentAim)
      {
         //Do we really want to create new ports here?


         //discharges = new List<BatteryDischarge>();

         if (!arduinoPort.IsOpen)
         {
            arduinoPort.PortName = portName;
            arduinoPort.BaudRate = 115200;
            arduinoPort.DtrEnable = true;
            //arduinoPort.DataReceived += arduinoBoard_DataReceived;
            try
            {
               arduinoPort.Open();
               if (arduinoPort.IsOpen)
               {
                  SetCurrentAim(currentAim);
                  arduinoPort.Write("G\n");
                  return true;
               }
            }
            catch (Exception ex)
            {
               MessageBox.Show(ex.Message);
               return false;
            }
            return false;
         }
         else
         {
            MessageBox.Show("Already open");
            return true;
         }
      }

      


      public bool Disconnect()
      {
         if (arduinoPort.IsOpen)
         {
            arduinoPort.Close();
            if (!arduinoPort.IsOpen)
            {
               arduinoPort.Write("O\n");
               return true;
            }
         }
         else
         {
            MessageBox.Show("Port not open");
            return true;
         }
         return false;
      }


      public void SetCurrentAim(double value)
      {
         if (arduinoPort.IsOpen)
         {
            arduinoPort.Write("c" + value.ToString("##"));
         }
      }

      public void Dispose()
      {
         try
         {
            arduinoPort.Close();
         }
         catch { }
         try
         {
            arduinoPort.Dispose();
         }
         catch { }
      }

   }
}
