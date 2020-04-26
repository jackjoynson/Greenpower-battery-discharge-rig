//Battery discharge code written by Jack Joynson 22/07/16 for use with control panel.

//TODO: IMPLEMENT NOBATTERY, NEWBATTERY AND ERROR LOGIC.
        //CHECK DISCHARGING, DISCHARGED AND OFF LOGIC.
        //IMPROVE ABOVE - START NEW DISCHARGE SHOULD RESET VALUES IF PREVIOUSLY DISCHARGED -NEW BATTERY?
        //RESETING AMPHOURS ON GO.



//CHANGE LOG:
//USER          DATE        COMMENTS
//Jack Joynson  22/07/2016  Create of new code to work with discharge board
//Jack Joynson  27/07/2016  Created constants for each variable to allow for easier calibration
//Jack Joynson  27/07/2016  Calibrated to watts up meter B
//Jack Joynson  31/08/2016  Calibrated to Power supply
//Jack Joynson  31/08/2016  Calibrated correctly to power supply, watts up meter and dvm. (Voltage averaging was removed too.)
//Michael McCoy 01/09/2016  Calibrated using P1, P2 and N5
//Tom Lawson    16/05/2017  Neatenimg and simplifying
//Tom Lawson    18/05/2017  Calibrating current against DC Load from MTP Process Dev
//Tom Lawson    02/06/2017  Calibration voltage against DMM
//Tom Lawson    09/06/2017  Calibration voltage against DMM
//Jack Joynson  23/07/2017  STARTED FROM V9 TO KEEP CALIBRATION VALUES - NEW LOGIC TO WORK WITH NEW DISCHARGE CONTROL PANEL
//Jack Joynson  29/07/2017  Added triangle wave generation
//#Include <PID_v1.h>
const double currentCal_M = 0.141771066; //was 0.148063776;
const double currentCal_C = 0.402; //was 0.2332; //was -1.4
const double voltageCal_M = 0.030516973; //was 0.030658;
const double voltageCal_C = -0.09327; //was 0.45313;
const int voltageCountsTillFail = 125;
const double minVoltage = 11.0;

const double PWMStep = 0.01;



// The arduinoStatus is sent as an integer and represents a bitwise status.
  // 1 - Discharged // 2 - Discharging // 4 - Off // 8 - No Battery // 16 - New Battery // 32 - Error
struct ArduinoStatus
{
  bool Discharged;
  bool Discharging;
  bool Off;
  bool NoBattery;
  bool NewBattery;
  bool Error;
};

ArduinoStatus arduinoStatus = {false,false,true,false,false,false};
//arduinoStatus.Discharged = false;
//arduinoStatus.Discharging = false;
//arduinoStatus.Off = true;
//arduinoStatus.NoBattery = false;
//arduinoStatus.NewBattery = false;
//arduinoStatus.Error = false;

const int VoltagePin = A0;
const int CurrentPin = A1;
const int PWMPin = 5;

//These are used for averaging
const int CurrentBufferSize = 100;
const int VoltageBufferSize = 50;

double MircoToHours = 1.0 / (1000.0 * 1000.0 * 60.0 * 60.0); //2.7777777777777777777777777777778e-10

double PWMDuty = 0;
double targetCurrent = 20.0;

double voltage;
double voltageBuffer[VoltageBufferSize];
double voltageRunningTotal;
bool voltageBufferFull = false;
int8_t voltageBufferPosition = 0;

double ampHours;
unsigned long EnergyUsagePrevTime = 0;

double current;
double currentBuffer[CurrentBufferSize];
double currentRunningTotal;
bool currentBufferFull = false;
int8_t currentBufferPosition = 0;

int countsThisCycle = 0;
int trianglePeriodCounts = 1000;
double triangleAmplitude = 3;
bool inTriangleMode = false;

int8_t underVoltageCount = 0;

int intervalDuration = 100;
unsigned long tickTime = 0;

double dischargeDuration = 0.0;
unsigned long prevDisDur;

int8_t diffDuration = 0;

bool go = false;

bool Running = false;


void setup()
{
  Serial.begin(115200);

  Serial.flush();

//this is setting the PWM frequency on Timer 2
  TCCR2B = TCCR2B & 0xF9; // was 0b11111000 | 0x01;

  for (int i = 0; i < CurrentBufferSize; i++)
  {
    currentBuffer[i] = 0;
  }

  for (int i = 0; i < VoltageBufferSize; i++)
  {
    voltageBuffer[i] = 0;
  }

  SetTick();

  pinMode(VoltagePin, INPUT);
  pinMode(CurrentPin, INPUT);
  pinMode(PWMPin, OUTPUT);
}

//this is the continously running main loop of the program
void loop()
{
  SerialReceive();
  ReadSensors();
//if battery is discharged, reset everything
  if (Discharged())
  {
    go = false;
    Running = false;
    arduinoStatus.Discharged = true;
    arduinoStatus.Discharging = false;
    arduinoStatus.Off = true;
    digitalWrite(PWMPin, LOW);
    PWMDuty = 0;
  }
  //if battery is not discharged
  else {
    if (go)
    {
      if (!Running)
      {
        Running = true;
        dischargeDuration = 0.0;
      }
    }
    else if (Running)
    {
      digitalWrite(PWMPin, LOW);
      PWMDuty = 0;
      Running = false;
    }
  }
  CurrentControl();
  if (TickElasped())
  {
    SerialTransmit();
  }
}

//adjust pwm duty until current matches target
void CurrentControl()
{
  if (Running)
  {
    double currentAim;
    if(inTriangleMode)
    {
        countsThisCycle++;
        if (countsThisCycle > trianglePeriodCounts) countsThisCycle = 0;
        
        int halfPeriod = trianglePeriodCounts/2;
        if(countsThisCycle < halfPeriod)
        {
          currentAim = (2*triangleAmplitude*countsThisCycle/halfPeriod) + targetCurrent - triangleAmplitude;
        }
        else
        {
          currentAim = targetCurrent + triangleAmplitude - (2*triangleAmplitude*(countsThisCycle-halfPeriod)/halfPeriod);
        }
    }
    else
    {
      currentAim = targetCurrent;
    }
    if (current > currentAim) PWMDown();     
    else if (current < currentAim) PWMUp();
    analogWrite(PWMPin, PWMDuty);
  }
}
//fucntion tests for battery being discharged (battery voltage < 11 volts)
bool Discharged()
{
  if (voltage < minVoltage)
  {
    if (underVoltageCount > voltageCountsTillFail)
    {
      return true;
      arduinoStatus.Discharged = true;
    }
    else
    {
      underVoltageCount++;
    }
  }
  else
  {
    underVoltageCount = 0;
  }

  return false;
}

void PWMUp()
{
  if (PWMDuty < 255)
  {
    PWMDuty += PWMStep;
  }
  else
  {
    PWMDuty = 255;
  }
}

void PWMDown()
{
  if (PWMDuty > 0)
  {
    PWMDuty -= PWMStep;
  }
  else
  {
    PWMDuty = 0;
  }
}

void ReadSensors()
{
  double tempCurrent = analogRead(CurrentPin);
  unsigned long currTime = micros();
  
  CurrentAveraging((tempCurrent <= currentCal_C) ? 0.0 : ((tempCurrent * currentCal_M) + currentCal_C)); 
  //this "?" operator is a ternery it means that
  //currentAveraging input is 0 if(tempCurrent is <= currentCal_C) else ((tempCurrent * currentCal_M) + currentCal_C)

   voltage = (analogRead(VoltagePin)*voltageCal_M) +voltageCal_C;


  //AMPHOURS IS CURRENT * CHANGE IN TIME.

  double EnergyAddition = current * ((currTime - EnergyUsagePrevTime) * MircoToHours);
  if (EnergyAddition > 0) //LOGIC ADDED TO CHECK THAT THE AMPHOURS ONLY ADDS AND DOESNT GET RESET. IF ARDUINO RESETS AND CURRTIME = 0 THEN THE LAST VALUE WOULD HAVE RESET THE TOTAL.
  {
    ampHours += EnergyAddition;
  }
  EnergyUsagePrevTime = currTime;
}

//this is system time
void SetTick()
{
  tickTime = millis() + intervalDuration;
}

bool TickElasped()
{
  if (tickTime < millis())
  {
    SetTick();
    return true;
  }
  else
  {
    return false;
  }
}

void CurrentAveraging(double input)
{
  double removedValue = currentBuffer[currentBufferPosition];
  
  currentBuffer[currentBufferPosition] = input;
  currentRunningTotal += input;

  if (currentBufferFull)
  {
    currentRunningTotal -= removedValue;
    if (currentBufferPosition == (CurrentBufferSize - 1))
    {
      currentBufferPosition = 0;
    }
    else
    {
      currentBufferPosition++;
    }

    current = currentRunningTotal / CurrentBufferSize;
    return;
  }
  else if (currentBufferPosition == (CurrentBufferSize - 1))
  {
    currentBufferFull = true;
    currentBufferPosition = 0;

    return;
  }
  else
  {
    currentBufferPosition++;
  }
}

void SerialReceive()
{
  if (Serial.available() > 0)
  {
    char firstVal = Serial.read();
    if (firstVal == 'g' || firstVal == 'G')
    {
      go = true;
      arduinoStatus.Discharged = false;
      arduinoStatus.Discharging = true;
      arduinoStatus.Off = false;
      inTriangleMode = false;
      
      //RESET VALUES
      ampHours = 0;
      underVoltageCount = 0;
      
      digitalWrite(13, HIGH);
    }
    else if(firstVal == 's' || firstVal == 'S')
    {
      go = true;
      arduinoStatus.Discharged = false;
      arduinoStatus.Discharging = true;
      arduinoStatus.Off = false;
      inTriangleMode = true;
      
      //RESET VALUES
      ampHours = 0;
      underVoltageCount = 0;
      
      digitalWrite(13, HIGH);
    }
    else if (firstVal == 'o' || firstVal == 'O')
    {
      go = false;
      arduinoStatus.Off = true;
      arduinoStatus.Discharging = false;
    }
    else if (firstVal == 'c' || firstVal == 'C')
    {
      int temp = Serial.parseInt();
      if (temp > 0)
      {
        targetCurrent = temp;
      }
    }
    else if(firstVal == 'p' || firstVal == 'P')
    {
      int temp = Serial.parseInt();
      if(temp > 0)
      {
        trianglePeriodCounts = temp;
      }
    }
    else if(firstVal == 'a' || firstVal == 'A')
    {
      double temp = Serial.parseFloat();
      if(temp> 0)
      {
         triangleAmplitude = temp; 
      }
    }
  }
}

void SerialTransmit()
{
  if (Running)
  {
    unsigned long curMillis = millis();
    unsigned long dif = curMillis - prevDisDur;
    if(dif > 0) dischargeDuration += dif;
    prevDisDur = curMillis;
  }
  Serial.print(GetArduinoStatusValue()); //Status
  Serial.print(",");
  Serial.print(current); 
  Serial.print(",");
  Serial.print(ampHours);
  Serial.print(",");
  Serial.print(voltage);
  Serial.print(",");
  Serial.print(targetCurrent);
  Serial.print(",");
  Serial.print(PWMDuty*100/255);
  Serial.print(",");
  Serial.println(dischargeDuration);
}

int GetArduinoStatusValue()
{
  int toReturn = 0;
  if(arduinoStatus.Discharged)toReturn += 1;
  if(arduinoStatus.Discharging)toReturn += 2;
  if(arduinoStatus.Off)toReturn += 4;
  if(arduinoStatus.NoBattery)toReturn += 8;
  if(arduinoStatus.NewBattery)toReturn += 16;
  if(arduinoStatus.Error)toReturn += 32;  
  return toReturn;
}
