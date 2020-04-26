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
using System.Reflection;
using Excel = Microsoft.Office.Interop.Excel;
using Word = Microsoft.Office.Interop.Word;
using Microsoft.Office.Core;
using Forms = System.Windows.Forms;
using System.IO;
using System.Diagnostics;

namespace BatteryLogPlotter
{
   /// <summary>
   /// Battery plotting tool by Jack Joynson.
   /// Scans network files then produces CSVs of final results
   /// </summary>
   public partial class MainWindow : Window
   {
      //Global variables
      string batteryLogsPath = @"\\UKNML7112\GREENPOWER\DISCHARGE RIG\Discharge Logs";
      string saveReportsPath = @"\\UKNML7112\GREENPOWER\DISCHARGE RIG\Reports";
      List<string> selectableFiles = new List<string>();
      List<string> selectableCodes = new List<string>();
      private bool showErrors = true;

      int minimumFileLines = 20;

      private enum cellColumns
      {
         status = 'A',
         current = 'B',
         amphours = 'C',
         voltage = 'D',
         aim = 'E',
         PWM = 'F',
         time = 'G'
      }

      private enum splitColumns
      {
         status,
         current,
         amphours,
         voltage,
         aim,
         PWM,
         time
      }

      private string[] columnNames = {"Status","Current","Amphours","Voltage","CurrentAim","PWM","Time"};
      private bool[] saveColumn = { false, false, true, true, true, false, true };


      private int numberColumns = 7;

      public MainWindow()
      {
         InitializeComponent();

         setDirectoryBox();


         populateDischargeFilesListBox();
         populateBatteryCodesListBox();
      }

      #region ************************************************************ initialisations methods **************************************************

      private void setDirectoryBox()
      {
         txtDirectory.Text = batteryLogsPath;
      }

      #endregion

      #region ************************************************************ button events ************************************************************

      private void btnGenerateSingleReport_Click(object sender, RoutedEventArgs e)
      {
         showSimpleDialog("This has not been completed yet");
         int selectedIndex = lstDischargeFiles.SelectedIndex;
         if (selectedIndex >= 0)
         {
            string name = (string)lstDischargeFiles.SelectedValue;
            createExcel(selectableFiles[selectedIndex], name);
         }
      }

      private void btnGenerateBatteryReport_Click(object sender, RoutedEventArgs e)
      {
         int selectedIndex = lstBatteryCodes.SelectedIndex;
         if (selectedIndex >= 0)
         {
            //showSimpleDialog(selectableCodes[selectedIndex]);
            generateBatteryReport(selectableCodes[selectedIndex]);
         }
      }

      private void btnBrowse_Click(object sender, RoutedEventArgs e)
      {
         Forms.FolderBrowserDialog folderBrowser = new Forms.FolderBrowserDialog();
         folderBrowser.Description = "Select the folder containing the battery discharge logs";
         folderBrowser.SelectedPath = batteryLogsPath;
         if (folderBrowser.ShowDialog() == System.Windows.Forms.DialogResult.OK)
         {
            if (Directory.Exists(folderBrowser.SelectedPath))
            {
               batteryLogsPath = folderBrowser.SelectedPath;
               setDirectoryBox();
               populateDischargeFilesListBox();
               populateBatteryCodesListBox();
            }
            else
            {
               showSimpleDialog("Could not find the selected directory", "Error", Forms.MessageBoxIcon.Error);
            }

         }
      }

      #endregion

      #region ************************************************************ General method ***********************************************************

      private void showSimpleDialog(string message, string title = "Message", Forms.MessageBoxIcon icon = Forms.MessageBoxIcon.None)
      {
         Forms.MessageBox.Show(message, title, Forms.MessageBoxButtons.OK, icon);
      }

      #endregion

      #region ************************************************************ Misc methods *************************************************************

      private void populateDischargeFilesListBox()
      {
         if (Directory.Exists(batteryLogsPath))
         {
            lstDischargeFiles.Items.Clear();
            selectableFiles.Clear();
            int length = batteryLogsPath.Length + 1;
            try
            {
               string[] files = Directory.GetFiles(batteryLogsPath);
               foreach (string file in files)
               {
                  if (file.Length > (4 + length) && file.Contains(".csv"))
                  {
                     string toAdd = file.Substring(length, file.IndexOf(".csv") - length);
                     lstDischargeFiles.Items.Add(toAdd);
                     selectableFiles.Add(file);
                  }
               }


            }
            catch (Exception err)
            {
               showSimpleDialog("An error occured fetching the files in: " + batteryLogsPath + ". Error: " + err, "File error", Forms.MessageBoxIcon.Error);
            }
         }
      }

      private void populateBatteryCodesListBox()
      {
         if (Directory.Exists(batteryLogsPath))
         {
            lstBatteryCodes.Items.Clear();
            selectableCodes.Clear();
            int length = batteryLogsPath.Length + 1;
            try
            {
               string[] files = Directory.GetFiles(batteryLogsPath);
               foreach (string file in files)
               {
                  string postDir = file.Substring(length);
                  if (postDir.Contains(' ') && postDir.Length > 4 && postDir.EndsWith(".csv"))
                  {
                     int spaceLoc = postDir.IndexOf(' ');
                     string toAdd = postDir.Substring(0, spaceLoc).ToUpper();
                     bool passed = true;
                     foreach (string code in selectableCodes)
                     {
                        if (toAdd == code)
                        {
                           passed = false;
                        }
                     }
                     if (passed)
                     {
                        selectableCodes.Add(toAdd);
                     }
                  }
               }

               selectableCodes.Sort();
               foreach (string item in selectableCodes)
               {
                  lstBatteryCodes.Items.Add(item);
               }
            }
            catch (Exception err)
            {
               showSimpleDialog("An error occured fetching the files in: " + batteryLogsPath + ". Error: " + err, "File error", Forms.MessageBoxIcon.Error);
            }
         }
      }
      #endregion


      private void allButton_Click(object sender, RoutedEventArgs e)
      {
         Process.Start(saveReportsPath);
         showErrors = false;
         for (int i = 0; i < selectableCodes.Count; i++) {
            generateBatteryReport(selectableCodes[i]);
         }
         showErrors = true;
      }


      private void generateBatteryReport(string code)
      {
         code += " ";
         string[] reports = new string[numberColumns];
         int pathLength = batteryLogsPath.Length + 1;
         string[] networkFiles = Directory.GetFiles(batteryLogsPath);
         foreach (string file in networkFiles)
         {
            string postDir = file.Substring(pathLength);
            if (postDir.StartsWith(code))
            {
               string lastData = getFileData(file);
               if (lastData.Length > 5)
               {
                  setReports(lastData, ref reports);
               }
               else
               {
                  lastData = getFileData(file, 5);
                  if(lastData.Length < 5)
                  {
                     if (showErrors)
                     {
                        showSimpleDialog("File lines all seem too small: " + file);
                     }
                  }
                  setReports(lastData, ref reports);
               }
            }
         }

         saveReports(reports, code);

         if (showErrors)
         {
            Process.Start(saveReportsPath);
         }
      }

      private void setReports(string data, ref string[] reports)
      {
         string[] splits = data.Split(',');
         if (splits.Length == numberColumns) {
            for (int i = 0; i < numberColumns; i++)
            {
               reports[i] += splits[i] +",";
            }
         }
         else
         {
            if (showErrors)
            {
               showSimpleDialog("Invalid number of deliminators: " + splits.Length);
            }
         }


      }

      private string getFileData(string file, int numberFromEnd = 2)
      {
         try
         {
            string[] lines = File.ReadAllLines(file);
            int length = lines.Length - numberFromEnd;
            if (length >= minimumFileLines) {
               return lines[length];
            }
            else
            {
               if (showErrors)
               {
                  showSimpleDialog("Not enough lines in: " + file + ". Only found: " + length);
               }
            }
         }
         catch(Exception err)
         {
            showSimpleDialog("Error accessing file: " + file + ". Error code: " + err);
         }
         return "";
      }

      private void saveReports(string[] reports, string code)
      {
         for (int i = 0; i < numberColumns; i++)
         {
            if (saveColumn[i])
            {
               try
               {
                  File.WriteAllText(saveReportsPath + @"\" + code + columnNames[i]+ " report.csv", reports[i]);
               }
               catch (Exception err)
               {
                  showSimpleDialog("An error occured creating the report: " + err);
               }
            }
         }
      }







      private void createExcel(string path, string name)
      {
         object oMissing = System.Reflection.Missing.Value;

         Excel.Application xlApp = new Excel.Application();
         Excel.Workbook xlWorkBook = xlApp.Workbooks.Open(path);
         Excel.Worksheet xlWorkSheet = (Excel.Worksheet)xlWorkBook.Worksheets.get_Item(1);

         xlApp.Visible = true;

         //createExcelChart(xlWorkSheet, oMissing, "A"); //Status graph

         cellColumns chartType = cellColumns.current;
         createExcelChart(xlWorkSheet, oMissing, chartType); //Current


         tryCreateDirectory(saveReportsPath);
         string saveName = getSavePath(saveReportsPath, "Excel save name", ".xls", name);
         showSimpleDialog(saveName);
         if (saveName != "" && saveName != null && saveName != name)
         {
            xlWorkBook.SaveAs(saveName, Excel.XlFileFormat.xlWorkbookNormal,
                oMissing, oMissing, oMissing, oMissing, Excel.XlSaveAsAccessMode.xlExclusive,
                oMissing, oMissing, oMissing, oMissing, oMissing);
         }

         xlWorkBook.Close(true, oMissing, oMissing);
         xlApp.Quit();

         releaseObject(xlWorkSheet);
         releaseObject(xlWorkBook);
         releaseObject(xlApp);
      }

      private void releaseObject(object obj)
      {
         try
         {
            if (obj != null)
            {
               System.Runtime.InteropServices.Marshal.ReleaseComObject(obj);
               obj = null;
            }
         }
         catch (Exception err)
         {
            obj = null;
            MessageBox.Show("Exception Occured while releasing object " + err.ToString());
         }
         finally
         {
            GC.Collect();
         }
      }

      private string getSavePath(string iniDirectory, string message, string ext, string name = "")
      {
         Forms.SaveFileDialog dialog = new Forms.SaveFileDialog();
         dialog.FileName = name;
         dialog.DefaultExt = ext;
         dialog.InitialDirectory = iniDirectory;
         dialog.Title = message;

         dialog.ShowDialog();
         string savePath = dialog.FileName;
         if (savePath == null || savePath.Length < 5)
         {
            savePath = getSavePath(iniDirectory, message, ext, name);
         }
         return savePath;
      }

      private void tryCreateDirectory(string path)
      {
         if (!Directory.Exists(path))
         {
            try
            {
               Directory.CreateDirectory(path);
            }
            catch (Exception err)
            {
               showSimpleDialog("An error occured creating the save reports directory (" + path + ") Error: " + err, "Directory error", Forms.MessageBoxIcon.Error);
               Environment.Exit(1);
            }
         }
      }


      private void createExcelChart(Excel.Worksheet xlWorkSheet, object oMissing, cellColumns type)
      {
         string rangeEndLetter = "" + (char)type;

         Excel.Range chartRange;

         Excel.ChartObjects xlCharts = (Excel.ChartObjects)xlWorkSheet.ChartObjects(Type.Missing);
         Excel.ChartObject myChart = (Excel.ChartObject)xlCharts.Add(10, 80, 300, 250);
         Excel.Chart chartPage = myChart.Chart;

         int rowCounts = xlWorkSheet.Rows.Count;
         string rangeEndConverted = "" + rangeEndLetter;
         string rangeEnd = rangeEndConverted + rowCounts;

         chartRange = xlWorkSheet.get_Range("A1", rangeEnd);
         chartPage.SetSourceData(chartRange, oMissing);
         chartPage.ChartType = Excel.XlChartType.xlLineMarkers;
      }

   }
}
