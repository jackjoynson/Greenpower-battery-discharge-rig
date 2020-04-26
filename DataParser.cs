using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DischargeControlPanel
{
   static class DataParser
   {

      public static ValueSet GetValues(string line)
      {
         ValueSet toReturn = new ValueSet();
         try
         {
            string[] splits = line.Split(',');

            toReturn.ArduinoStatus = GetArduinoStatus(splits[0]);
            toReturn.Current = double.Parse(splits[1]);
            toReturn.AmpHours = double.Parse(splits[2]);
            toReturn.Voltage = double.Parse(splits[3]);
            toReturn.CurrentLimit = double.Parse(splits[4]);
            toReturn.PWMDuty = double.Parse(splits[5]);
            toReturn.DischargeDuration = double.Parse(splits[6]);
         }
         catch (Exception) { }
         return toReturn;
      }

      public static ValueSet.ArduinoStatusTemplate GetArduinoStatus(string status)
      {
         try
         {
            ValueSet.ArduinoStatusTemplate toReturn = new ValueSet.ArduinoStatusTemplate();
            int statusInt = int.Parse(status);

            if (statusInt >= 32)
            {
               toReturn.Error = true;
               statusInt -= 32;
            }
            else toReturn.Error = false;
            if (statusInt >= 16)
            {
               toReturn.NewBattery = true;
               statusInt -= 16;
            }
            else toReturn.NewBattery = false;
            if (statusInt >= 8)
            {
               toReturn.NoBattery = true;
               statusInt -= 8;
            }
            else toReturn.NoBattery = false;
            if (statusInt >= 4)
            {
               toReturn.Off = true;
               statusInt -= 4;
            }
            else toReturn.Off = false;
            if (statusInt >= 2)
            {
               toReturn.Discharging = true;
               statusInt -= 2;
            }
            else toReturn.Discharging = false;
            if (statusInt == 1)
            {
               toReturn.Discharged = true;
               statusInt -= 1;
            }
            else toReturn.Discharged = false;

            return toReturn;
         }
         catch(Exception)
         {

         }
         return new ValueSet.ArduinoStatusTemplate();
      }

   }
}
