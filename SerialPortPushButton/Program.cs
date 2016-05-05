using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.IO.Ports;
using System.IO.Pipes;

// Doku:
// http://tech.pro/tutorial/752/dotnet-35-adds-named-pipes-support  

namespace SerialPortPushButton
{
  class Program
  {
    static SerialPort p;
    static StreamWriter sw;
    static string version;
    static bool prevState;

    static void Main(string[] args)
    {
      version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();

      // first connect to named-pipe-server to put data there.
      NamedPipeClientStream pipeClient = new NamedPipeClientStream(".","TIMclk",PipeDirection.Out);
      Console.Write("Attempting to connect to pipe... ");
      pipeClient.Connect();
      Console.WriteLine("Connected to pipe.");
      Console.WriteLine("There are currently {0} pipe server instances open.", pipeClient.NumberOfServerInstances);

      if (pipeClient.IsConnected == false)
      {
        Console.WriteLine("Pipe is not connected. Aborting. Please start Server first.");
        return;
      }

      sw = new StreamWriter(pipeClient);
      sw.AutoFlush = true;

      try
      {
        string sendtext = "SPPB-HELO:Serial Port Push Button Client version " + version;
        Console.WriteLine(sendtext);
        sw.WriteLine(sendtext);
      }
      // Catch the IOException that is raised if the pipe is 
      // broken or disconnected.
      catch (IOException e)
      {
        Console.WriteLine("ERROR: {0}", e.Message);
      }

      p = new SerialPort("COM10", 9600, Parity.None, 8, StopBits.One);
      //p.Handshake = Handshake.RequestToSend;
      p.RtsEnable = true; // set pin 7 to use as VCC
      //p.DtrEnable = true;
      p.PinChanged += new SerialPinChangedEventHandler(myPinChanged);

      try
      {
        p.Open();
      }
      catch (Exception e)
      {
        Console.WriteLine("Cannot open Port. Try again. (Press Enter to quit) ...");
        Console.ReadLine();
        return;
      }

      Console.WriteLine("Connected to COM1.");

      //p.Write("huh");
      //char[] mybuf = new char[100];
      //p.Read(mybuf, 0, 99);

      // TODO: entprellung mit totzeit
      // mehrere true,true,true,false,false,false hintereinander möglich.

      Console.ReadLine();
    }

    static void myPinChanged(object sender, SerialPinChangedEventArgs e)
    {
      //DateTime dtNow = DateTime.Now;
      TimeSpan tsNow = DateTime.Now.TimeOfDay;

      string info = string.Empty;
      switch (e.EventType)
      {
        case SerialPinChange.Break:
          info = string.Format("Break detected. BreakState = {0}", p.BreakState);
          break;
        case SerialPinChange.CDChanged:
          // this is called when connecting pin 7 (VCC with RtsEnable) to pin 1 (Data Carrier Detect)
          info = string.Format("Carrier Detect (CD) signal changed state. CD State = {0}", p.CDHolding);
          break;
        case SerialPinChange.CtsChanged:
          info = string.Format("Clear to Send (CTS) signal changed state. CTS State = {0}", p.CtsHolding);
          break;
        case SerialPinChange.DsrChanged:
          // this is called when connecting 7 (VCC with RTSEnable) to pin 6 (Data Set Ready)
          //info = string.Format("Data Set Ready (DSR) signal changed state. DSR State = {0}", p.DsrHolding);
          string statstr = p.DsrHolding == true ? "ON" : "OFF";
          //string writestr = "SPPB-EV:001 " + statstr + " " + dtNow.ToString() + "," + dtNow.Millisecond.ToString().PadLeft(3, '0');
          string writestr = "SPPB-EV:001 " + statstr + " " + tsNow.ToString(@"hh\:mm\:ss\,fff");
          try
          {
            sw.WriteLine(writestr);
            Console.WriteLine(writestr);
          }
          catch (Exception ex)
          {
            Console.WriteLine("ERROR ON: "+writestr);
            Console.WriteLine(ex.ToString());
          }
          break;
        case SerialPinChange.Ring:
          info = "Ring detected";
          break;
        default:
          break;
      }
      Console.WriteLine(info);

      // rising edge detection
      bool thisState = p.CDHolding;
      if (thisState == true && prevState == false)
      {
        // edge detected
      }
    }
  }
}
