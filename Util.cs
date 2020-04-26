using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Speech.Synthesis;
using Microsoft.Win32;
using System.Diagnostics;

namespace DischargeControlPanel
{
   class Utilities
   {
      SpeechSynthesizer synthesizer;
      public Utilities()
      {
         init(logPath);
         init(comPath);
         initDefDir();
         synthesizer = new SpeechSynthesizer();
         synthesizer.Volume = 100;  // 0...100
         synthesizer.Rate = -2;     // -10...10
      }

      public string logPath = @"C:\ProgramData\JJS\DischargeRig\Log.txt";
      public string comPath = @"C:\ProgramData\JJS\DischargeRig\COM.txt";
      public string defDirPath = @"C:\ProgramData\JJS\DischargeRig\DefDir.txt";

      void init(string path)
      {
         if (!File.Exists(path))
         {
            if (!Directory.Exists(Path.GetDirectoryName(path)))
            {
               Directory.CreateDirectory(Path.GetDirectoryName(path));
            }
         }
      }

      void initDefDir()
      {
         if (!File.Exists(defDirPath))
         {
            if (!Directory.Exists(Path.GetDirectoryName(defDirPath)))
            {
               Directory.CreateDirectory(Path.GetDirectoryName(defDirPath));
            }
            File.WriteAllText(defDirPath, @"C:\GREENPOWER\DISCHARGE RIG\Discharge Logs\");
         }
      }

      public string GetCOMPort()
      {
         if (File.Exists(comPath)) return File.ReadAllText(comPath);
         else return "COM12";
      }

      public void SetCOMPort(string port)
      {
         try
         {
            File.WriteAllText(comPath, port);
         }
         catch (Exception err)
         {
            Log("AN ERROR OCCURED SAVING THE COM PORT TO A FILE: " + err);
         }
      }

      public string GetSavePath()
      {
         string saveLocation = File.ReadAllText(defDirPath);

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

      public void Log(string text)
      {
         try
         {
            File.AppendAllText(logPath, text + " (" + DateTime.Now + ")" + Environment.NewLine);
         }
         catch (Exception err)
         {
            MessageBox.Show("Error writing to error log file: " + err + Environment.NewLine);
         }
      }

      public void Speak(string text)
      {
         synthesizer.SpeakAsync(text);
      }

      public void LaunchPortal()
      {
         Process.Start(@"https://portal.gptiming.net/");
      }

   }
}
