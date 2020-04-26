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

namespace Gauge
{
   /// <summary>
   /// Interaction logic for UserControl1.xaml
   /// </summary>
   public partial class GaugeControl : UserControl
   {
      public GaugeControl()
      {
         InitializeComponent();
      }
      public double MinValue { get; set; }
      public double MaxValue { get; set; }

      public void SetValue(double value)
      {
         //Convert value to percentage of min and max value
         double percent = (value - MinValue) / (MaxValue - MinValue);

         //Convert percent to degrees
         double startAngle = arcBase.StartAngle;
         double endAngle = arcBase.EndAngle;

         arcValue.EndAngle = percent * (endAngle - startAngle) - endAngle;
         txtValue.Text = value.ToString();
      }


      public void SetRegionValue(int region, double start, double end, Brush colour)
      {
         //Convert value to percentage of min and max value
         double percentStart = (start - MinValue) / (MaxValue - MinValue);
         double percentEnd = (end - MinValue) / (MaxValue - MinValue);

         //Convert percent to degrees
         double startAngle = arcBase.StartAngle;
         double endAngle = arcBase.EndAngle;

         double arcStartValue = percentStart * (endAngle - startAngle) - endAngle;
         double arcEndValue = percentEnd * (endAngle - startAngle) - endAngle;

         Microsoft.Expression.Shapes.Arc ControlToUse;
         switch (region)
         {
            case 1:
               ControlToUse = valueRange1;
               break;
            case 2:
               ControlToUse = valueRange2;
               break;
            case 3:
               ControlToUse = valueRange3;
               break;
            case 4:
               ControlToUse = valueRange4;
               break;
            case 5:
               ControlToUse = valueRange5;
               break;
            default:
               return;
         }

         ControlToUse.StartAngle = arcStartValue;
         ControlToUse.EndAngle = arcEndValue;

         ControlToUse.Stroke = colour;
      }

   }
}
