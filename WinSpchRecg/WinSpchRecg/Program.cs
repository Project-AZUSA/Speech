using System;
using System.Text;
using System.Speech.Recognition;


namespace WinSpchRecg
{
    class Program
    {
       

        //識別引擎
        static SpeechRecognitionEngine _recognizer = new SpeechRecognitionEngine();
        
        static void _recognizer_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            Console.WriteLine("INPUT(\"" + e.Result.Text + "\")");
        }

        static int AZUSAPid=-1;

        static void Main(string[] args)
        {


            while(AZUSAPid==-1)
            {
                Console.WriteLine("GetAzusaPid()");
                try
                {
                    AZUSAPid = Convert.ToInt32(Console.ReadLine());
                    break;
                }
                catch
                {
                }
            }


            Console.WriteLine("RegisterAs(Input)");

            _recognizer.SetInputToDefaultAudioDevice();
            _recognizer.SpeechRecognized += new EventHandler<SpeechRecognizedEventArgs>(_recognizer_SpeechRecognized);
            _recognizer.LoadGrammar(new DictationGrammar());

            while (true)
            {
                try
                {
                    System.Diagnostics.Process.GetProcessById(AZUSAPid);                    
                }
                catch
                {
                    Environment.Exit(0);
                }

                _recognizer.Recognize();
            }

        }
    }
}
