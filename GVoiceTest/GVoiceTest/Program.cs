using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CloudSpeech;
using System.Runtime.InteropServices;
using System.Reflection;
using System.IO;
using System.Speech.Recognition;

namespace GVoiceTest
{
    class Program
    {
        [DllImport("winmm.dll")]
        private static extern int mciSendString(string lpstrCommand, string lpstrReturnString, int uReturnLength, int hwndCallback);


        static SpeechToText _recg = new SpeechToText();

        static void Main(string[] args)
        {
            SpeechRecognitionEngine _eng = new SpeechRecognitionEngine();
            _eng.SetInputToDefaultAudioDevice();
            _eng.SpeechRecognized += new EventHandler<SpeechRecognizedEventArgs>(_eng_SpeechRecognized);

            _eng.LoadGrammar(new DictationGrammar());

            while (true)
            {
                //開始錄音
                mciSendString("open new type waveaudio alias recsound", "", 0, 0);
                mciSendString("set recsound bitspersample 16 channels 1 alignment 2 samplespersec 16000 bytespersec 32000 format tag pcm wait", "", 0, 0);
                mciSendString("record recsound", "", 0, 0);
                _eng.Recognize();
            }

        }

        static void _eng_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            Console.WriteLine("Speech detected, processing...");

            //保存錄音
            mciSendString("pause recsound", null, 0, 0);

            mciSendString("save recsound " + Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\spch.wav", null, 0, 0);
            mciSendString("close recsound", null, 0, 0);

            //解析
            var resp = _recg.Recognize(File.OpenRead(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\spch.wav"));
            if (resp.Count() != 0)
            {
                Console.WriteLine("You said: " + resp.First().Utterance);
            }
            else
            {
                Console.WriteLine("Unknown");
            }

        }
    }
}
