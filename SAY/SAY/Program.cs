using System;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace SAY
{
    class Program
    {
        static string emotion = "";
        static bool noAnimation = false;
        static void Main(string[] args)
        {
            if (args.Length == 0) { return; }

            string arg = args[0].Trim();
            int id = 0;
            if (Int32.TryParse(arg.Split(',')[0], out id))
            {

                Speak(id, arg.Replace(id + ",", ""));
            }
            else
            {
                Speak(id, arg);
            }


        }

        static void Speak(int id, string content)
        {


            string[] lines;
            #region decide emotion
            //get current emotion
            Console.WriteLine("EMOTION?");
            emotion = Console.ReadLine();

            //if emotion doesn't exist, set it to empty
            if (emotion == "EMOTION")
            {
                emotion = "";
            }

            //load emotion settings
            try
            {
                lines = System.IO.File.ReadAllLines(Environment.CurrentDirectory + @"\EMOTION.txt");
            }
            catch
            {
                Console.WriteLine(@"ERR(Unable to load the emotion settings. [SAY])");
                Console.Out.Flush();
                return;
            }

            //decide emotion
            int numline = 1;
            foreach (string line in lines)
            {
                if (line.Trim() != "" || !line.StartsWith("#")) //allows some formatting
                {
                    string[] separators = { "," };
                    string[] entry = line.Split(separators, StringSplitOptions.RemoveEmptyEntries);
                    try
                    {
                        if (emotion == entry[0])
                        {
                            File.WriteAllText(Environment.CurrentDirectory + @"\spchGen.bat", "cd %~dp0\n" + entry[1]);
                        }
                    }
                    catch
                    {
                        Console.WriteLine(@"ERR(Unable to parse line " + numline.ToString() + " of the emotion settings. [SAY])");
                    }
                }
                numline++;
            }
            #endregion

            #region coordinate animation
            foreach (string part in content.Split(new char[] { '、', ' ', '\t' }))
            {
                if (part.Trim() != "")
                {

                    //Generate speech wav                  

                    if (!GenSpeech(part)) { return; }

                    
                    if (!noAnimation)
                    {
                        Console.WriteLine(@"UI_PlaySound(res\sound\speech.wav," + id + ")");
                    }
                    else
                    {
                        using (System.Media.SoundPlayer player = new System.Media.SoundPlayer(Environment.CurrentDirectory + @"\res\sound\speech.wav"))
                        {
                            player.PlaySync();
                        }
                    }
                }
            }
            #endregion
        }

        static bool GenSpeech(string text)
        {
            string txt;
            if (text.StartsWith("*"))
            {
                noAnimation = true;
                txt = text.TrimStart('*');
            }
            else
            {
                noAnimation = false;
                txt = text;
            }

            File.WriteAllText(Environment.CurrentDirectory + @"\text.txt", txt, Encoding.GetEncoding(932));

            Process JTalk = new Process();
            JTalk.StartInfo.CreateNoWindow = true;
            JTalk.StartInfo.UseShellExecute = false;
            JTalk.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            JTalk.StartInfo.FileName = "cmd.exe";
            JTalk.StartInfo.Arguments = "/c \"" + Environment.CurrentDirectory + "\\spchGen.bat\"";


            try
            {
                JTalk.Start();

                JTalk.WaitForExit();
            }
            catch
            {
                return false;
            }
             
            return true;
        }
    }
}
