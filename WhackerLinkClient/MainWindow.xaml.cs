using System.Windows;
using WebSocketSharp;
using NAudio.Wave;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

using WhackerLinkCommonLib.Models;
using WhackerLinkCommonLib.Models.IOSP;
using System.ComponentModel;

#nullable disable

namespace WhackerLinkClient
{
    public partial class MainWindow : Window
    {
        private WebSocket _socket;
        private readonly WaveInEvent _waveIn;
        private readonly WaveOutEvent _waveOut;
        private readonly BufferedWaveProvider _waveProvider;
        private bool _isKeyedUp;

        private string myRid = "123456";
        private string currentTgid = "15001";

        private string currentChannel = string.Empty;
        private bool isRegistered = false;
        private bool powerOn = false;

        private TaskCompletionSource<bool> deregistrationCompletionSource;

        public MainWindow()
        {
            InitializeComponent();
            Application.Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;


            try
            {
                _socket = new WebSocket("ws://localhost:3009/client");
                _socket.OnOpen += Socket_OnOpen;
                _socket.OnClose += Socket_OnClose;
                _socket.OnMessage += Socket_OnMessage;
                _socket.OnError += Socket_OnError;

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

                _socket.Connect();

                PowerOn();
                
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

        private void Socket_OnError(object sender, ErrorEventArgs e)
        {
            Console.WriteLine($"WebSocket error: {e.Message}");
        }

        private void Socket_OnMessage(object sender, MessageEventArgs e)
        {
            var data = JObject.Parse(e.Data);
            var type = Convert.ToInt32(data["type"]);

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

        private void PowerOn()
        {
            if (powerOn)
                return;

            sendUnitRegistrationRequest();
            sendGroupAffiliationRequest();

            powerOn = true;
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

        private void sendUnitDeRegistration()
        {
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
            if (!powerOn || !isRegistered)
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

        private async void Window_Closing(object sender, CancelEventArgs e)
        {
            try
            {
                deregistrationCompletionSource = new TaskCompletionSource<bool>();
                sendUnitDeRegistration();

                await deregistrationCompletionSource.Task;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred during closing: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PlayAudio(byte[] audioData)
        {
            _waveProvider.AddSamples(audioData, 0, audioData.Length);
            _waveOut.Play();
        }
    }
}