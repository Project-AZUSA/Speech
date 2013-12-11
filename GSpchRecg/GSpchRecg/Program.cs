using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using NAudio.Wave;

namespace GSpchRecg
{
    class Program
    {
        static float threshold = 0.1f;
        static WaveFileWriter writer;
        static WaveFormat WavFormat = new WaveFormat(16000, 1);
        static bool speech = false;
        static bool recording = false;

        static string lang = "ja";

        static void Main(string[] args)
        {
            WaveInEvent WavEvent = new WaveInEvent();
            WavEvent.DeviceNumber = 0;

            WavEvent.DataAvailable += new EventHandler<WaveInEventArgs>(InputDevice_DataAvailable);
            WavEvent.WaveFormat = WavFormat;
            WavEvent.StartRecording();

            while (true)
            {
                if (speech)
                {
                    System.Threading.Thread.Sleep(1000);
                }
                else if (!speech && recording)
                {
                    Console.WriteLine("Processing");

                    try
                    {
                        writer.Flush();
                        writer.Close();
                        recording = false;
                    }
                    catch
                    {
                        Console.WriteLine("ERR(IO Exception [GSR])");
                    }
                    SpeechToText();


                }
            }

        }

        static void InputDevice_DataAvailable(object sender, WaveInEventArgs e)
        {
            for (int i = 0; i < e.BytesRecorded; i += 2)
            {
                short sample = (short)((e.Buffer[i + 1] << 8) |
                                        e.Buffer[i + 0]);

                speech = false;
                if (sample / 32768f > threshold)
                {
                    speech = true;
                    break;
                }
            }

            if (recording)
            {
                writer.Write(e.Buffer, 0, e.BytesRecorded);
            }
            else
            {
                if (speech)
                {
                    speech = true;


                    Console.WriteLine("Speech Detected");
                    try
                    {
                        writer = new WaveFileWriter(Environment.CurrentDirectory + @"\tmp.wav", WavFormat);
                        writer.Write(e.Buffer, 0, e.BytesRecorded);
                        recording = true;
                    }
                    catch
                    {
                        Console.WriteLine("ERR(IO Exception [GSR])");
                    }
                }
            }
        }

        static void SpeechToText()
        {
            byte[] wavTmp=new byte[1];

            try
            {
                wavTmp= File.ReadAllBytes(Environment.CurrentDirectory + @"\tmp.wav");
            }
            catch
            {
                Console.WriteLine("ERR(IO Exception [GSR])");
                return;
            }

            try
            {
                WebRequest GoogleSAPI = WebRequest.Create("http://www.google.com/speech-api/v1/recognize?xjerr=1&client=chromium&lang=" + lang + "&maxresults=1");
                GoogleSAPI.Method = "POST";
                GoogleSAPI.ContentType = "audio/L16; rate=16000";
                GoogleSAPI.ContentLength = wavTmp.Length;

                Stream dataStream = GoogleSAPI.GetRequestStream();
                dataStream.Write(wavTmp, 0, wavTmp.Length);
                dataStream.Close();

                WebResponse resp = GoogleSAPI.GetResponse();
                dataStream = resp.GetResponseStream();
                StreamReader respReader = new StreamReader(dataStream);

                Console.WriteLine(GetUtterance(respReader.ReadToEnd()));

                resp.Close();
            }
            catch
            {
                Console.WriteLine("ERR(Cannot retrieve response from Google [GSR])");
            }
        }

        static string GetUtterance(string content)
        {
            if (content.IndexOf("utterance") == -1)
            {
                return "SPCHREJ";
            }
            return content.Substring(content.IndexOf("utterance") + 12, content.IndexOf("confidence") - content.IndexOf("utterance") - 15);
        }

    }
}
