using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DischargeControlPanel
{
   public class ValueSet
   {
      public double Current;
      public double Voltage;
      public double AmpHours;
      public double CurrentLimit;
      public double PWMDuty;
      public double DischargeDuration;

      public ArduinoStatusTemplate ArduinoStatus;

      public struct ArduinoStatusTemplate
      {
         public bool Discharging;
         public bool Discharged;
         public bool Off;
         public bool NoBattery;
         public bool NewBattery;
         public bool Error;
      }
   }
}
