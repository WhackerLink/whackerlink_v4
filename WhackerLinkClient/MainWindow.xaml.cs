using System.Windows;
using WebSocketSharp;
using NAudio.Wave;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

using WhackerLinkCommonLib.Models;
using WhackerLinkCommonLib.Models.IOSP;
using WhackerLinkCommonLib.Models.Radio;
using System.ComponentModel;
using System.Windows.Input;

using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using System.IO;

#nullable disable

namespace WhackerLinkClient
{
    public partial class MainWindow : Window
    {
        private WebSocket _socket;
        private readonly WaveInEvent _waveIn;
        private readonly WaveOutEvent _waveOut;
        private readonly BufferedWaveProvider _waveProvider;
        private Codeplug codeplug;
        private bool _isKeyedUp;

        private string myRid;
        private string currentTgid;
        private int currentZoneIndex = 0;
        private int currentChannelIndex = 0;
        private Codeplug.System currentSystem;

        private string currentChannel = string.Empty;
        private bool isRegistered = false;
        private bool powerOn = false;
        private ErrorStates errorState = ErrorStates.NONE;

        private TaskCompletionSource<bool> deregistrationCompletionSource;

        public MainWindow()
        {
            InitializeComponent();
            this.MouseLeftButtonDown += (sender, e) => DragMove();

            Application.Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;

            try
            {
                _waveIn = new WaveInEvent
                {
                    WaveFormat = new WaveFormat(8000, 16, 1)
                };
                _waveIn.DataAvailable += WaveIn_DataAvailable;

                _waveOut = new WaveOutEvent();
                _waveProvider = new BufferedWaveProvider(new WaveFormat(8000, 16, 1))
                {
                    DiscardOnBufferOverflow = true
                };
                _waveOut.Init(_waveProvider);                
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while initializing: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Current_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show($"Unhandled exception: {e.Exception.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }


        private void Socket_OnOpen(object sender, EventArgs e)
        {
            Console.WriteLine("Connected to the server.");
        }

        private void Socket_OnClose(object sender, CloseEventArgs e)
        {
            Console.WriteLine("Disconnected from the server.");
        }

        private void Socket_OnError(object sender, WebSocketSharp.ErrorEventArgs e)
        {
            Console.WriteLine($"WebSocket error: {e.Message}");
        }

        private void Socket_OnMessage(object sender, MessageEventArgs e)
        {
            var data = JObject.Parse(e.Data);
            var type = Convert.ToInt32(data["type"]);

            if (!powerOn && errorState == ErrorStates.NONE && _socket != null)
                return;

            switch (type)
            {
                case (int)PacketType.U_REG_RSP:
                    HandleUnitRegistrationResponse(data["data"].ToObject<U_REG_RSP>());
                    break;
                case (int)PacketType.U_DE_REG_RSP:
                    HandleUnitDeRegistrationResponse(data["data"].ToObject<U_DE_REG_RSP>());
                    break;
                case (int)PacketType.GRP_AFF_RSP:
                    HandleGroupAffiliationResponse(data["data"].ToObject<GRP_AFF_RSP>());
                    break;
                case (int)PacketType.GRP_VCH_RSP:
                    HandleVoiceChannelResponse(data["data"].ToObject<GRP_VCH_RSP>());
                    break;
                case (int)PacketType.GRP_VCH_RLS:
                    HandleVoiceChannelRelease(data["data"].ToObject<GRP_VCH_RLS>());
                    break;
                case (int)PacketType.AUDIO_DATA:
                    PlayAudio(data["data"].ToObject<byte[]>());
                    break;
                default:
                    Console.WriteLine("Unknown message type: " + type);
                    break;
            }
        }

        private Codeplug LoadCodeplug(string path)
        {
            try
            {
                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .Build();

                var yaml = File.ReadAllText(path);
                return deserializer.Deserialize<Codeplug>(yaml);
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        private void PTTButton_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (!powerOn || !isRegistered)
                return;

            if (!_isKeyedUp)
            {
                _isKeyedUp = true;
                var request = new
                {
                    type = (int)PacketType.GRP_VCH_REQ,
                    data = new GRP_VCH_REQ
                    {
                        SrcId = myRid,
                        DstId = currentTgid,
                    }
                };
                _socket.Send(JsonConvert.SerializeObject(request));
            }
        }

        private void MasterConnection(Codeplug.System system)
        {
            if (_socket != null)
            {
                if (_socket.IsAlive)
                {
                    _socket.Close();
                    _socket = null;
                }
            }

            try
            {
                _socket = new WebSocket($"ws://{system.Address}:{system.Port}/client");
                _socket.OnOpen += Socket_OnOpen;
                _socket.OnClose += Socket_OnClose;
                _socket.OnMessage += Socket_OnMessage;
                _socket.OnError += Socket_OnError;

                _socket.Connect();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to connect to WebSocket: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void KillMasterConnection()
        {
            if (_socket != null)
            {
                _socket.Close();
            }
        }

        private void PowerOn()
        {
            codeplug = LoadCodeplug("codeplug.yml");

            if (codeplug == null)
            {
                errorState = ErrorStates.CPG_CHECKSUM;
                txt_Line2.Text = "Fail 01/82";
                return;
            }

            currentZoneIndex = 0;
            currentChannelIndex = 0;

            UpdateDisplay();
        }

        private void PowerOff()
        {
            sendUnitDeRegistrationRequest();
            ClearFields();
            isRegistered = false;
        }

        private void sendUnitRegistrationRequest()
        {
            var request = new
            {
                type = (int)PacketType.U_REG_REQ,
                data = new U_REG_REQ
                {
                    SrcId = myRid,
                    SysId = "",
                    Wacn = ""
                }
            };

            _socket.Send(JsonConvert.SerializeObject(request));
        }

        private void sendUnitDeRegistrationRequest()
        {
            if (!_socket.IsAlive)
                return;

            var request = new
            {
                type = (int)PacketType.U_DE_REG_REQ,
                data = new GRP_AFF_REQ
                {
                    SrcId = myRid,
                    DstId = currentTgid,
                    SysId = ""
                }
            };

            _socket.Send(JsonConvert.SerializeObject(request));
        }

        private void sendGroupAffiliationRequest()
        {
            var request = new
            {
                type = (int)PacketType.GRP_AFF_REQ,
                data = new GRP_AFF_REQ
                {
                    SrcId = myRid,
                    DstId = currentTgid,
                    SysId = ""
                }
            };

            _socket.Send(JsonConvert.SerializeObject(request));
        }

        private void PTTButton_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (!powerOn || !isRegistered && errorState == ErrorStates.NONE)
                return;

            if (_isKeyedUp)
            {
                _isKeyedUp = false;
                var request = new
                {
                    type = (int)PacketType.GRP_VCH_RLS,
                    data = new GRP_VCH_RLS
                    {
                        SrcId = myRid,
                        DstId = currentTgid,
                        Channel = currentChannel
                    }
                };
                _socket.Send(JsonConvert.SerializeObject(request));
                _waveIn.StopRecording();
            }
        }

        private void WaveIn_DataAvailable(object sender, WaveInEventArgs e)
        {
            if (_isKeyedUp)
            {
                var audioData = new
                {
                    type = (int)PacketType.AUDIO_DATA,
                    data = e.Buffer
                };
                _socket.Send(JsonConvert.SerializeObject(audioData));
            }
        }

        private void HandleGroupAffiliationResponse(GRP_AFF_RSP response)
        {
            if (!isRegistered || !powerOn)
                return;

            // Do nothing for now I guess
        }

        private void HandleUnitRegistrationResponse(U_REG_RSP response)
        {
            isRegistered = false;

            if (response.SrcId != myRid)
                return;

            if (response.Status == (int)ResponseType.GRANT)
            {
                isRegistered = true;
            }
            else if (response.Status == (int)ResponseType.REFUSE)
            {
                Dispatcher.Invoke(() => txt_Line1.Text = "Sys reg refusd");
                isRegistered = false;
            }
            else
            {
                isRegistered = false;
            }
        }

        private void HandleUnitDeRegistrationResponse(U_DE_REG_RSP response)
        {
            if (response.SrcId == myRid)
            {
                deregistrationCompletionSource?.SetResult(true);
            }
        }

        private void HandleVoiceChannelRelease(GRP_VCH_RLS response)
        {
            if (!powerOn || !isRegistered || (response.DstId != currentTgid))
                return;

            bool isMe = response.SrcId == myRid;

            if (!isMe)
                _waveOut.Pause();
        }

        private void HandleVoiceChannelResponse(GRP_VCH_RSP response)
        {
            if (!powerOn || !isRegistered || (response.DstId != currentTgid))
                return;

            bool isMe = response.SrcId == myRid;

            try
            {
                if (response.Status == (int)ResponseType.GRANT && isMe)
                {
                    currentChannel = response.Channel;
                    Console.WriteLine($"Channel granted: {response.Channel}");
                    _waveIn.StartRecording();
                }
                else if (!isMe)
                {
                    currentChannel = string.Empty;
                    Console.WriteLine("Channel request denied.");
                }
                else
                {
                    currentChannel = response.Channel;
                    _waveOut.Play();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing JSON response: {ex.Message}");
                Console.WriteLine($"Response content: {response}");
            }
        }

        private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                PTTButton_MouseDown(sender, null);
                e.Handled = true;
            }
        }

        private void MainWindow_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                PTTButton_MouseUp(sender, null);
                e.Handled = true;
            }
        }

        private async void Window_Closing(object sender, CancelEventArgs e)
        {
            try
            {
                if (_socket != null && _socket.IsAlive)
                {
                    deregistrationCompletionSource = new TaskCompletionSource<bool>();
                    sendUnitDeRegistrationRequest();

                    await deregistrationCompletionSource.Task;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred during closing: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearFields()
        {
            txt_Line1.Text = "";
            txt_Line2.Text = "";
            txt_Line3.Text = "";
        }

        private void PlayAudio(byte[] audioData)
        {
            _waveProvider.AddSamples(audioData, 0, audioData.Length);
            _waveOut.Play();
        }

        private void btn_Power_Click(object sender, RoutedEventArgs e)
        {
            if (!powerOn)
                PowerOn();
            else
                PowerOff();

            powerOn = !powerOn;
        }

        private void btn_ZoneUp_Click(object sender, RoutedEventArgs e)
        {
            ChangeZone(1);
        }

        private void btn_ZoneDown_Click(object sender, RoutedEventArgs e)
        {
            ChangeZone(-1);
        }

        private void btn_ChangeChannel_Click(object sender, RoutedEventArgs e)
        {
            ChangeChannel();
        }

        private void ChangeZone(int direction)
        {
            if (!powerOn || codeplug == null)
                return;

            currentZoneIndex += direction;

            if (currentZoneIndex < 0)
                currentZoneIndex = codeplug.Zones.Count - 1;
            else if (currentZoneIndex >= codeplug.Zones.Count)
                currentZoneIndex = 0;

            currentChannelIndex = 0;
            UpdateDisplay();
        }

        private void ChangeChannel()
        {
            if (!powerOn || codeplug == null)
                return;

            var zone = codeplug.Zones[currentZoneIndex];
            currentChannelIndex++;

            if (currentChannelIndex >= zone.Channels.Count)
                currentChannelIndex = 0;

            UpdateDisplay(false);
        }

        private void UpdateDisplay(bool systemChange = true)
        {
            if (codeplug != null && codeplug.Zones.Count > 0)
            {
                var zone = codeplug.Zones[currentZoneIndex];
                txt_Line1.Text = zone.Name;

                if (zone.Channels.Count > 0)
                {
                    var channel = zone.Channels[currentChannelIndex];
                    txt_Line2.Text = channel.Name;
                    currentTgid = channel.Tgid;
                    var newSystem = codeplug.GetSystemForChannel(channel);

                    if (!powerOn || (currentSystem == null || currentSystem.Name != newSystem.Name))
                    {
                        currentSystem = newSystem;
                        myRid = currentSystem.Rid;

                        KillMasterConnection();
                        MasterConnection(currentSystem);

                        if (systemChange)
                            sendUnitRegistrationRequest();
                    }

                    sendGroupAffiliationRequest();
                }
            }
        }
    }
}