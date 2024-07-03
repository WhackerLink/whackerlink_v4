using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using NAudio.Wave;
using Newtonsoft.Json;
using WhackerLinkCommonLib.Handlers;
using WhackerLinkCommonLib.Interfaces;
using WhackerLinkCommonLib.Models;
using WhackerLinkCommonLib.Models.IOSP;
using WhackerLinkCommonLib.Models.Radio;
using WhackerLinkCommonLib.UI;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

#nullable disable

namespace WhackerLinkMobileRadio
{
    public partial class MainWindow : Window, IRadioDisplay
    {
        private readonly IWebSocketHandler _webSocketHandler;
        private readonly RadioDisplayUpdater _radioDisplayUpdater;
        private readonly WaveInEvent _waveIn;
        private readonly WaveOutEvent _waveOut;
        private readonly BufferedWaveProvider _waveProvider;

        private Codeplug _codeplug;
        private string _myRid;
        private string _currentTgid;
        private int _currentZoneIndex;
        private int _currentChannelIndex;
        private Codeplug.System _currentSystem;
        private bool _isRegistered;
        private bool _isInRange = false;
        private bool _powerOn;
        private bool _isKeyedUp;
        private string _currentChannel;
        private bool _isRecording = false;
        private bool _isReceiving = false;
        private bool _isInMenu = false;

        private TaskCompletionSource<bool> _deregistrationCompletionSource;
        private DispatcherTimer _reconnectTimer;

        public MainWindow()
        {
            InitializeComponent();

            this.MouseLeftButtonDown += (sender, e) => DragMove();

            Application.Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;

            _webSocketHandler = new WebSocketHandler();
            _radioDisplayUpdater = new RadioDisplayUpdater(this);

            _webSocketHandler.OnUnitRegistrationResponse += HandleUnitRegistrationResponse;
            _webSocketHandler.OnUnitDeRegistrationResponse += HandleUnitDeRegistrationResponse;
            _webSocketHandler.OnGroupAffiliationResponse += HandleGroupAffiliationResponse;
            _webSocketHandler.OnVoiceChannelResponse += HandleVoiceChannelResponse;
            _webSocketHandler.OnVoiceChannelRelease += HandleVoiceChannelRelease;
            _webSocketHandler.OnEmergencyAlarmResponse += HandleEmergencyAlarmResponse;
            _webSocketHandler.OnAudioData += PlayAudio;
            _webSocketHandler.OnOpen += HandleConnectionOpen;
            _webSocketHandler.OnClose += HandleConnectionClose;

            _waveIn = new WaveInEvent
            {
                WaveFormat = new WaveFormat(8000, 16, 1)
            };
            _waveIn.DataAvailable += WaveIn_DataAvailable;
            _waveIn.RecordingStopped += _waveIn_RecordingStopped;

            _waveOut = new WaveOutEvent();
            _waveProvider = new BufferedWaveProvider(new WaveFormat(8000, 16, 1))
            {
                DiscardOnBufferOverflow = true
            };
            _waveOut.Init(_waveProvider);

            _reconnectTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5)
            };
            _reconnectTimer.Tick += ReconnectTimer_Tick;
        }

        public bool PowerOn => _powerOn;
        public bool IsInRange { get => _isInRange; set => _isInRange = value; }
        public string MyRid { get => _myRid; set => _myRid = value; }
        public string CurrentTgid { get => _currentTgid; set => _currentTgid = value; }
        public Codeplug.System CurrentSystem { get => _currentSystem; set => _currentSystem = value; }

        private void Current_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            //MessageBox.Show($"Unhandled exception: {e.Exception.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
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
                Console.WriteLine($"Error loading codeplug: {ex.Message}");
                return null;
            }
        }

        private async void PTTButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!_powerOn) return;

            if (!_isRegistered || _isReceiving)
            {
                BeepGenerator.Bonk();
                return;
            }

            if (!_isKeyedUp)
            {
                _isKeyedUp = true;

                Dispatcher.Invoke(() => SetRssiSource("TX_RSSI.png"));
                await Task.Delay(350);
                Dispatcher.Invoke(() => SetRssiSource(""));
                await Task.Delay(200);

                var request = new
                {
                    type = (int)PacketType.GRP_VCH_REQ,
                    data = new GRP_VCH_REQ
                    {
                        SrcId = _myRid,
                        DstId = _currentTgid,
                    }
                };
                _webSocketHandler.SendMessage(request);
            }
        }

        public void KillMasterConnection()
        {
            _webSocketHandler.Disconnect();
            _isInRange = false;
        }

        public void MasterConnection(Codeplug.System system)
        {
            _webSocketHandler.Connect(system.Address, system.Port);

            if (!_webSocketHandler.IsConnected)
            {
                _isInRange = false;
            } else
            {
                _isInRange = true;
            }
        }

        public void SendUnitRegistrationRequest()
        {
            var request = new
            {
                type = (int)PacketType.U_REG_REQ,
                data = new U_REG_REQ
                {
                    SrcId = _myRid,
                    SysId = "",
                    Wacn = ""
                }
            };
            _webSocketHandler.SendMessage(request);
        }

        public async void SendGroupAffiliationRequest()
        {
            await Task.Delay(1000);

            if (!_isRegistered) return;

            var request = new
            {
                type = (int)PacketType.GRP_AFF_REQ,
                data = new GRP_AFF_REQ
                {
                    SrcId = _myRid,
                    DstId = _currentTgid,
                    SysId = ""
                }
            };
            _webSocketHandler.SendMessage(request);
        }

        private void SendUnitDeRegistrationRequest()
        {
            if (!_webSocketHandler.IsConnected) return;

            var request = new
            {
                type = (int)PacketType.U_DE_REG_REQ,
                data = new GRP_AFF_REQ
                {
                    SrcId = _myRid,
                    DstId = _currentTgid,
                    SysId = ""
                }
            };
            _webSocketHandler.SendMessage(request);
        }

        private void SendEmergencyAlarmRequest()
        {
            if (!_webSocketHandler.IsConnected || !_powerOn || !_isRegistered) return;

            var request = new
            {
                type = (int)PacketType.EMRG_ALRM_REQ,
                data = new EMRG_ALRM_REQ
                {
                    SrcId = _myRid,
                    DstId = _currentTgid
                }
            };
            _webSocketHandler.SendMessage(request);
        }

        private void PTTButton_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!_powerOn || !_isRegistered || _isReceiving) return;

            if (_isKeyedUp)
            {
                _isKeyedUp = false;
                var request = new
                {
                    type = (int)PacketType.GRP_VCH_RLS,
                    data = new GRP_VCH_RLS
                    {
                        SrcId = _myRid,
                        DstId = _currentTgid,
                        Channel = _currentChannel
                    }
                };
                _webSocketHandler.SendMessage(request);
                _waveIn.StopRecording();
            }
        }

        private void _waveIn_RecordingStopped(object sender, StoppedEventArgs e)
        {
            _isRecording = false;
        }

        private void WaveIn_DataAvailable(object sender, WaveInEventArgs e)
        {
            if (_isKeyedUp)
            {
                var audioData = new
                {
                    type = (int)PacketType.AUDIO_DATA,
                    data = e.Buffer,
                    voiceChannel = new VoiceChannel
                    {
                        SrcId = _myRid,
                        DstId = _currentTgid,
                        Frequency = _currentChannel
                    }
                };
                _webSocketHandler.SendMessage(audioData);
            }
        }

        private void HandleUnitRegistrationResponse(U_REG_RSP response)
        {
            string text = string.Empty;

            switch (response.Status)
            {
                case (int)ResponseType.GRANT:
                    _isRegistered = true;
                    break;
                case (int)ResponseType.REFUSE:
                default:
                    _isRegistered = false;
                    text = "Sys reg refusd";
                    break;
            }

            Dispatcher.Invoke(() => SetRssiSource("RSSI_COLOR_4.png"));
            Dispatcher.Invoke(() => txt_Line3.Text = text);
        }

        private void HandleUnitDeRegistrationResponse(U_DE_REG_RSP response)
        {
            _deregistrationCompletionSource?.SetResult(true);
        }

        private void HandleGroupAffiliationResponse(GRP_AFF_RSP response)
        {
            Dispatcher.Invoke(() => SetRssiSource("RSSI_COLOR_4.png"));
        }

        private async void HandleVoiceChannelRelease(GRP_VCH_RLS response)
        {
            if (!_powerOn || !_isRegistered || (response.DstId != _currentTgid)) return;

            _currentChannel = string.Empty;
            Dispatcher.Invoke(() => txt_Line3.Text = "");
            Dispatcher.Invoke(() => SetRssiSource(""));
            _isReceiving = false;
            await Task.Delay(250);
            Dispatcher.Invoke(() => SetRssiSource("RSSI_COLOR_4.png"));
        }

        private async void HandleVoiceChannelResponse(GRP_VCH_RSP response)
        {
            if (!_powerOn || !_isRegistered) return;

            if (response.Status == (int)ResponseType.GRANT && response.SrcId == _myRid && response.DstId == _currentTgid)
            {
                _currentChannel = response.Channel;
                await Task.Delay(100);
                Dispatcher.Invoke(() => SetRssiSource("TX_RSSI.png"));
                BeepGenerator.TptGenerate();

                if (!_isRecording)
                {
                    _isRecording = true;
                    _waveIn.StartRecording();
                }
            }
            else if (response.Status == (int)ResponseType.DENY && response.SrcId == _myRid && response.DstId == _currentTgid)
            {
                _currentChannel = string.Empty;
                if (_isKeyedUp) BeepGenerator.Bonk();
                Console.WriteLine("Channel request denied.");
            }
            else if (response.DstId == _currentTgid && response.SrcId != _myRid)
            {
                _currentChannel = response.Channel;
                Dispatcher.Invoke(() => txt_Line3.Text = $"ID: {response.SrcId}");
                Dispatcher.Invoke(() => SetRssiSource("RX_COLOR.png"));
            }
        }

        private void HandleEmergencyAlarmResponse(EMRG_ALRM_RSP response)
        {
            Dispatcher.Invoke(() => SetLine3Text($"EM: {response.SrcId}"));
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
            if (_webSocketHandler != null)
            {
                _deregistrationCompletionSource = new TaskCompletionSource<bool>();
                SendUnitDeRegistrationRequest();
                await _deregistrationCompletionSource.Task;
            }
        }

        private void EnterPageMenu()
        {
            _isInMenu = true;
            ClearFields(true);

            txt_SoftMenu1.Text = "Send";
            txt_SoftMenu4.Text = "Exit";
            
            Dispatcher.Invoke(() => SetLine1Text("Enter ID", true));
            Dispatcher.Invoke(() => SetLine2Text(string.Empty, true));
            Dispatcher.Invoke(() => SetLine3Text(string.Empty, true));

            txt_Line2.IsReadOnly = false;
        }

        private void ExitPageMenu()
        {
            ClearFields(true);
            txt_Line2.IsReadOnly = true;
            _isInMenu = false;
            SetupSoftButtons();
            _radioDisplayUpdater.UpdateDisplay(_codeplug, 0, 0, true);
        }

        private void SetupSoftButtons()
        {
            txt_SoftMenu1.Text = "PAGE";
            txt_SoftMenu4.Text = string.Empty;
        }

        private void ClearFields(bool forMenu = false)
        {
            if (_isInMenu && !forMenu) return;

            SetLine1Text("");
            SetLine2Text("");
            SetLine3Text("");

            if (!forMenu)
                SetRssiSource("");
        }

        public void SetLine1Text(string text, bool forMenu = false)
        {
            if (_isInMenu && !forMenu) return;

            txt_Line1.Text = text;
        }

        public void SetLine2Text(string text, bool forMenu = false)
        {
            if (_isInMenu && !forMenu) return;

            txt_Line2.Text = text;
        }

        public void SetLine3Text(string text, bool forMenu = false)
        {
            if (_isInMenu && !forMenu) return;

            txt_Line3.Text = text;
        }

        public void SetRssiSource(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                icon_Rssi.Source = null;
                return;
            }

            BitmapImage bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri($"pack://application:,,,/Resources/{name}");
            bitmap.EndInit();

            icon_Rssi.Source = bitmap;
        }

        private void PlayAudio(byte[] audioData, VoiceChannel voiceChannel)
        {
            if (string.IsNullOrEmpty(_currentChannel)) return;

            if (voiceChannel.Frequency == _currentChannel && voiceChannel.DstId == _currentTgid && voiceChannel.SrcId != _myRid)
            {
                _isReceiving = true;
                _waveProvider.AddSamples(audioData, 0, audioData.Length);
                _waveOut.Play();
            }
        }

        private void btn_Power_Click(object sender, RoutedEventArgs e)
        {
            if (!_powerOn)
                TogglePowerOn();
            else
                PowerOff();

            _powerOn = !_powerOn;
        }

        private void btn_Emerg_Click(object sender, RoutedEventArgs e)
        {
            SendEmergencyAlarmRequest();
        }

        private void TogglePowerOn()
        {
            _codeplug = LoadCodeplug("codeplug.yml");

            if (_codeplug == null)
            {
                Dispatcher.Invoke(() => txt_Line2.Text = "Fail 01/82");
                return;
            }

            _currentZoneIndex = 0;
            _currentChannelIndex = 0;

            SetupSoftButtons();

            _radioDisplayUpdater.UpdateDisplay(_codeplug, _currentZoneIndex, _currentChannelIndex);
        }

        private void btn_Emerg_MouseDown(object sender, MouseButtonEventArgs e)
        {
            SetLine3Text(null);
            e.Handled = true;
        }

        private void PowerOff()
        {
            SendUnitDeRegistrationRequest();
            ClearFields();
            txt_SoftMenu1.Text = string.Empty;
            txt_SoftMenu4.Text = string.Empty;
            _isRegistered = false;
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
            if (!_powerOn || _codeplug == null) return;

            _currentZoneIndex += direction;
            if (_currentZoneIndex < 0) _currentZoneIndex = _codeplug.Zones.Count - 1;
            else if (_currentZoneIndex >= _codeplug.Zones.Count) _currentZoneIndex = 0;

            _currentChannelIndex = 0;
            _radioDisplayUpdater.UpdateDisplay(_codeplug, _currentZoneIndex, _currentChannelIndex);
        }

        private void ChangeChannel()
        {
            if (!_powerOn || _codeplug == null) return;

            var zone = _codeplug.Zones[_currentZoneIndex];
            _currentChannelIndex++;
            if (_currentChannelIndex >= zone.Channels.Count) _currentChannelIndex = 0;

            _radioDisplayUpdater.UpdateDisplay(_codeplug, _currentZoneIndex, _currentChannelIndex);
        }

        private void HandleConnectionOpen()
        {
            if (_powerOn)
                Dispatcher.Invoke(() => SetRssiSource("RSSI_COLOR_4.png"));

            _isInRange = true;
            _reconnectTimer.Stop();
        }

        private void HandleConnectionClose()
        {
            if (_powerOn)
            {
                Dispatcher.Invoke(() => SetRssiSource("RSSI_COLOR_0.png"));
                Dispatcher.Invoke(() => txt_Line3.Text = "Out of Range");
            }

            _isInRange = false;
            _reconnectTimer.Start();
        }

        private void ReconnectTimer_Tick(object sender, EventArgs e)
        {
            if (!_webSocketHandler.IsConnected && _currentSystem != null)
            {
                KillMasterConnection();
                MasterConnection(_currentSystem);
                SendUnitRegistrationRequest();
                SendGroupAffiliationRequest();
            }
        }

        private void btn_SoftMenu1_Click(object sender, RoutedEventArgs e)
        {
            if (!_isInMenu)
                EnterPageMenu();
            //else
                //SendUnitToUnitPage();
        }

        private void btn_SoftMenu4_Click(object sender, RoutedEventArgs e)
        {
            if (_isInMenu)
                ExitPageMenu();
            else
                BeepGenerator.Bonk();
        }
    }
}