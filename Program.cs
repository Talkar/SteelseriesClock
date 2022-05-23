using System;
using System.IO;
using System.Net;
using System.Timers;
using Newtonsoft.Json;
using System.Runtime.InteropServices;
using System.Diagnostics;
using static System.Net.Mime.MediaTypeNames;

namespace Time_Steelseries
{
    public class CoreProps
    {
        public string address { get; set; }
        public string encrypted_address { get; set; }
        public CoreProps(string _address, string _encrypted_address)
        {
            address = _address;
            encrypted_address = _encrypted_address;
        }
    }
    class Program
    {
        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        static HttpWebRequest gameEventRequest;
        static Timer timer = new Timer(TimeSpan.FromSeconds(1).TotalMilliseconds);
        static int numberOfLoops = 1;
        static string coreProps; // The coreProps file with the ip and port information in string format
        static string ipAddress;
        static bool timerInitialized = false;
        static bool hideOutput = false;
        static public void Main(string[] args)
        {
            coreProps = File.ReadAllText("C:/ProgramData/SteelSeries/SteelSeries Engine 3/coreProps.json");
            CoreProps corePropsDeserialized = JsonConvert.DeserializeObject<CoreProps>(coreProps);
            ipAddress = "http://" + corePropsDeserialized.address;
            var parsedArg = -1;
            foreach (var arg in args)
            {

                int.TryParse(arg, out parsedArg);
                if (arg == "/q")
                {
                    IntPtr handle = Process.GetCurrentProcess().MainWindowHandle;
                    // Hide
                    ShowWindow(handle, 0);
                    hideOutput = true;

                }
            }

            Menu(parsedArg);
        }

        static void Menu(int argumentMenuItem = -1)
        {
            int response;
            if (argumentMenuItem >= 0)
            {
                response = argumentMenuItem;
            }
            else
            {
                while (true)
                {
                    WriteLine("What would you like to do?\n" +
                    "0 - Start Sending Clock Updates to engine\n" +
                    "1 - Install Engine App\n" +
                    "2 - Uninstall Engine App", false);
                    bool success = Int32.TryParse(Console.ReadLine(), out response);
                    if (success)
                    {
                        break;
                    }
                    else
                    {
                        WriteLine("The inputed value is not an integer, try again");
                    }
                }
            }
            switch (response)
            {
                case 0:
                    WriteLine("Starting UpdateClock Loop... Press any key to end the loop");
                    StartUpdateClockLoop();
                    break;
                case 1:
                    RegisterMetadata();
                    Menu();
                    break;
                case 2:
                    RemoveGame();
                    Menu();
                    break;
                default:
                    Menu();
                    break;
            }
        }

        static void WriteLine(string line, bool doClear = false)
        {
            if (hideOutput)
                return;

            if (doClear)
                Console.Clear();

            Console.WriteLine(line);
        }

        static void StartUpdateClockLoop()
        {
            if (!timerInitialized)
            {
                timerInitialized = true;

                timer.Elapsed += UpdateClock;
                timer.AutoReset = true;
            }
            timer.Enabled = true;

            Console.ReadKey(true);
            timer.Enabled = false;
            numberOfLoops = 1;
            Menu();
        }

        static void UpdateClock(Object source, ElapsedEventArgs e)
        {
            try
            {
                gameEventRequest = (HttpWebRequest)WebRequest.Create($"{ipAddress}/game_event");
                gameEventRequest.ContentType = "application/json";
                gameEventRequest.Method = "POST";

                WriteLine($"Running UpdateClock for the {numberOfLoops} time");
                numberOfLoops++;
                using (StreamWriter streamWriter = new StreamWriter(gameEventRequest.GetRequestStream()))
                {
                    string json = "{\"game\": \"CLOCK\"," +
                                  "\"event\": \"TIME\"," +
                                  "\"data\": { \"value\": 1, " +
                                  "\"frame\": {\"textvalue\": \"" + DateTime.Now.ToString("dd/MM/yy HH:mm") + "\"} } }";

                    streamWriter.Write(json);
                }
                HttpWebResponse gameEventResponse = (HttpWebResponse)gameEventRequest.GetResponse();
                WriteLine($"Response: {gameEventResponse.StatusCode}");
                gameEventRequest.Abort();
                gameEventResponse.Close();
            }
            catch (Exception exception)
            {
                WriteLine(exception.Message);
            }
        }

        static void BindEvent()
        {
            try
            {
                HttpWebRequest bindRequest = (HttpWebRequest)WebRequest.Create($"{ipAddress}/bind_game_event");
                bindRequest.ContentType = "application/json";
                bindRequest.Method = "POST";

                using (StreamWriter streamWriter = new StreamWriter(bindRequest.GetRequestStream()))
                {
                    string json = "{\"game\": \"CLOCK\", " +
                        "\"event\": \"TIME\", " +
                        "\"min_value\": 0, " +
                        "\"max_value\": 1, " +
                        "\"icon_id\": 15, " +
                        "\"value_optional\": true, " +
                        "\"handlers\": [{" +
                        "\"device-type\": \"screened\", " +
                        "\"mode\": \"screen\", " +
                        "\"zone\": \"one\", " +
                        "\"datas\": [{" +
                        "\"has-text\": true, " +
                        "\"context-frame-key\": \"textvalue\", " +
                        "\"icon-id\": 15" +
                        "}]}]}";

                    streamWriter.Write(json);
                }
                HttpWebResponse bindResponse = (HttpWebResponse)bindRequest.GetResponse();
                WriteLine($"Response: {bindResponse.StatusCode}");
                WriteLine("The event is bound");
            }
            catch (Exception exception)
            {
                WriteLine(exception.Message);
            }
        }

        static void RegisterMetadata()
        {
            try
            {
                HttpWebRequest registerRequest = (HttpWebRequest)WebRequest.Create($"{ipAddress}/game_metadata");
                registerRequest.ContentType = "application/json";
                registerRequest.Method = "POST";

                using (StreamWriter streamWriter = new StreamWriter(registerRequest.GetRequestStream()))
                {
                    string json = "{\"game\": \"CLOCK\", " +
                        "\"game_display_name\": \"Clock\", " +
                        "\"developer\": \"TechnOllieG & minor modifications by Talkar\"}";

                    streamWriter.Write(json);
                }
                HttpWebResponse registerResponse = (HttpWebResponse)registerRequest.GetResponse();
                WriteLine($"Response: {registerResponse.StatusCode}");
                WriteLine("The metadata has been registered");
            }
            catch (Exception exception)
            {
                WriteLine(exception.Message);
            }
            BindEvent();
        }

        static void RemoveGame()
        {
            try
            {
                HttpWebRequest removalRequest = (HttpWebRequest)WebRequest.Create($"{ipAddress}/remove_game");
                removalRequest.ContentType = "application/json";
                removalRequest.Method = "POST";

                using (StreamWriter streamWriter = new StreamWriter(removalRequest.GetRequestStream()))
                {
                    string json = "{\"game\": \"CLOCK\"}";

                    streamWriter.Write(json);
                }
                HttpWebResponse removalResponse = (HttpWebResponse)removalRequest.GetResponse();
                WriteLine($"Response: {removalResponse.StatusCode}");
                WriteLine("The app has been uninstalled");
            }
            catch (Exception exception)
            {
                WriteLine(exception.Message);
            }
        }
    }
}
