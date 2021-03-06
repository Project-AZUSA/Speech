﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using ZMQ;
using NAudio.Wave;

namespace GSpchRecg
{
    class Program
    {
        //引擎的名稱, 向用戶提示用
        static string NAME = "Google Speech Recognition";

        //引擎會嘗試取得一個沒有被佔用的 TCP 端口進行消息發報
        //失敗的話會重試
        //這個是重試次數
        static int RETRY = 3;

        //這個是預設的端口
        //如果已被佔用會換成別的端口
        static int DEFAULT_PORT = 1111;

        static float threshold = 0.1f;
        static WaveFileWriter writer;
        static WaveFormat WavFormat = new WaveFormat(16000, 1);
        static bool speech = false;
        static bool recording = false;

        static string lang = "ja";

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

        static string SpeechToText()
        {
            byte[] wavTmp=new byte[1];

            try
            {
                wavTmp= File.ReadAllBytes(Environment.CurrentDirectory + @"\tmp.wav");
            }
            catch
            {
                Console.WriteLine("ERR(IO Exception [GSR])");
                return "";
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
                string content = respReader.ReadToEnd();
                resp.Close();

                return GetUtterance(content);                
            }
            catch
            {
                Console.WriteLine("ERR(Cannot retrieve response from Google [GSR])");
                return "";
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

        

        //這是引擎的初始化的部分,如果成功請返回 true, 如果失敗請返回 false
        static bool Initialize()
        {
            WaveInEvent WavEvent = new WaveInEvent();
            WavEvent.DeviceNumber = 0;

            WavEvent.DataAvailable += new EventHandler<WaveInEventArgs>(InputDevice_DataAvailable);
            WavEvent.WaveFormat = WavFormat;
            WavEvent.StartRecording();

            Console.WriteLine("SYS_LANG?");
            lang = Console.ReadLine().ToLower();

            Console.WriteLine("GSR_THRESHOLD?");
            if(!float.TryParse(Console.ReadLine(),out threshold)){
                threshold = 0.1f;
            }

            if (lang == "sys_lang")
            {
                lang = "ja";
            }

            return true;
        }

        //這是取得輸入的部分, 這個方法會被不斷循環呼叫, 如有需要可以加入 Thread.Sleep 設置時間間隔
        //請返回需要向 AI 發佈的消息
        static string GetInput()
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
                    recording = false;
                    writer.Flush();
                    writer.Close();
                }
                catch
                {
                    Console.WriteLine("ERR(IO Exception [GSR])");
                }
                return SpeechToText();
            }
            return "";
        }


        //接下來是處理與 AZUSA 和其他引擎溝通的部分, 一般不需要改動
        //========================================
        #region Communications
        static int AZUSAPid;

        static void Main(string[] args)
        {


            //初始化失敗的話退出並發出通知
            if (!Initialize())
            {
                Console.WriteLine("ERR(" + NAME + " failed to initialize.)");
                return;
            }

            Console.WriteLine("GetAzusaPid()");
            AZUSAPid = Convert.ToInt32(Console.ReadLine());

            Console.WriteLine("RegisterAs(Input)");

            //創建 zmq PUB 端口
            using (Context ctx = new Context())
            using (Socket server = ctx.Socket(SocketType.PUB))
            {
                //記錄是否已成功創建
                bool SUCCES = false;

                //暫存當前嘗試連接的端口, GetPort 返回一個沒在 AZUSA 上登記的端口
                string PORT = GetPort();

                //暫存已經嘗試過失敗的端口
                string LastTried = "";

                //反覆嘗試 RETRY 次
                for (int i = 0; i < RETRY; i++)
                {
                    //嘗試連接
                    try
                    {
                        //如果上一次已經試過失敗了, 就試下一個端口
                        if (PORT == LastTried)
                        {
                            PORT = (Convert.ToInt32(PORT) + 1).ToString();
                        }

                        //連接
                        server.Bind("tcp://127.0.0.1:" + PORT);

                        //連接成功的話就向 AZUSA 登錄端口
                        Console.WriteLine("RegisterPort(\"tcp://127.0.0.1:" + PORT + "\")");

                        //然後 SUCCES 設為 true 表示成功
                        SUCCES = true;

                        //退出循環
                        break;
                    }
                    catch
                    {
                        //否則失敗的話就記錄到 LastTried, 那麼下一次就不會再嘗試連接同一個端口了
                        LastTried = PORT;
                    }
                }

                //如果失敗的話退出並發出通知
                if (!SUCCES)
                {
                    Console.WriteLine("ERR(" + NAME + " was not able to obtain a vacant port. It has been terminated.)");
                    return;
                }

                //暫存消息
                string msg;

                //循環取得輸入, 並利用 zmq 端口發佈消息
                while (true)
                {
                    //檢查 AZUSA 是否存活
                    try
                    {
                        System.Diagnostics.Process.GetProcessById(AZUSAPid);
                    }
                    catch
                    {
                        return;
                    }

                    msg = GetInput();

                    if (msg != "")
                    {
                        server.Send(msg, Encoding.UTF8);
                    }

                }
            }

        }




        //根據 AZUSA 的回應, 返回一個沒在 AZUSA 上登記的端口
        static string GetPort()
        {
            //先向 AZUSA 提取所有已登記的端口
            Console.WriteLine("GetAllPorts()");

            //進行解析
            string[] busyPorts = Console.ReadLine().Split(',');

            //暫存可使用的端口, 設為預設值
            int PORT = DEFAULT_PORT;

            try
            {
                //逐個端口檢查, 找出最大的端口
                foreach (string port in busyPorts)
                {
                    if (Convert.ToInt32(port.Split(':')[1]) > PORT)
                    {
                        PORT = Convert.ToInt32(port.Split(':')[1]);
                    }
                }

                //然後加一
                PORT++;
            }
            catch
            {
                //否則的話就甚麼都不做, 直接返回預設值
            }


            return PORT.ToString();
        }

        #endregion

    }
}
