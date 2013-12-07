using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CloudSpeech;
using System.Runtime.InteropServices;
using System.Reflection;
using System.IO;

namespace GVoiceTest
{
    class Program
    {

        [DllImport("winmm.dll")]
        private static extern int mciSendString(string lpstrCommand, string lpstrReturnString, int uReturnLength, int hwndCallback);



        
        static void Main(string[] args)
        {
            SpeechToText _recg = new SpeechToText();

            while (true)
            {
                
                Console.WriteLine("Press enter to start recording...");
                Console.ReadLine();

                //開始錄音
                mciSendString("open new type waveaudio alias recsound", "", 0, 0);
                mciSendString("set recsound bitspersample 16 channels 1 alignment 2 samplespersec 16000 bytespersec 32000 format tag pcm wait", "", 0, 0);
                mciSendString("record recsound", "", 0, 0);

                Console.WriteLine("Recording... Press enter to stop.");
                Console.ReadLine();

                //保存錄音
                mciSendString("pause recsound", null, 0, 0);

                mciSendString("save recsound " + Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\spch.wav", null, 0, 0);
                mciSendString("close recsound", null, 0, 0);

                //解析
                TextResponse resp = _recg.Recognize(File.OpenRead(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\spch.wav")).First();

                Console.WriteLine("You said: " + resp.Utterance);

                Console.ReadLine();

            }
        } 
    }
}
