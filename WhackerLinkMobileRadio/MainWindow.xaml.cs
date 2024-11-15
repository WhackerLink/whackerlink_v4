/*
* WhackerLink - WhackerLinkMobileRadio
*
* This program is free software: you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation, either version 3 of the License, or
* (at your option) any later version.
*
* This program is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
* GNU General Public License for more details.
*
* You should have received a copy of the GNU General Public License
* along with this program.  If not, see <http://www.gnu.org/licenses/>.
* 
* Copyright (C) 2024 Caleb, K4PHP
* 
*/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using NAudio.Wave;
using Newtonsoft.Json;
using WhackerLinkLib.Handlers;
using WhackerLinkLib.Models.IOSP;
using WhackerLinkLib.Utils;
using WhackerLinkLib.Interfaces;
using WhackerLinkLib.Models;
using WhackerLinkLib.Models.Radio;
using WhackerLinkLib.UI;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using WhackerLinkCommonLib.Utils;

#nullable disable

namespace WhackerLinkMobileRadio
{
    /// <summary>
    /// MainWindow
    /// </summary>
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
        private Site _currentSite;

        private TaskCompletionSource<bool> _deregistrationCompletionSource;
        private DispatcherTimer _reconnectTimer;

        private readonly DispatcherTimer _pttCooldownTimer;
        private bool _isPttCooldown;

        private const int VK_PTT = 0x20;
        private readonly DispatcherTimer _pttWatchdogTimer;
        private readonly TimeSpan _pttMaxDuration = TimeSpan.FromSeconds(3000);
        private DateTime _pttStartTime;
        private bool _isPttActive;
        private bool _pttState;

        private static IntPtr _hookID = IntPtr.Zero;
        private NativeMethods.LowLevelKeyboardProc _proc;

        /// <summary>
        /// Creates an instance of MainWindow
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;

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
            _webSocketHandler.OnCallAlert += HandleCallAlert;
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

            _pttCooldownTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(2000)
            };
            _pttCooldownTimer.Tick += PttCooldownTimer_Tick;
            _isPttCooldown = false;

            _pttWatchdogTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            _pttWatchdogTimer.Tick += PttWatchdogTimer_Tick;
            _pttWatchdogTimer.Start();
        }

        public bool PowerOn => _powerOn;
        public bool IsInRange { get => _isInRange; set => _isInRange = value; }
        public string MyRid { get => _myRid; set => _myRid = value; }
        public string CurrentTgid { get => _currentTgid; set => _currentTgid = value; }
        public Site CurrentSite { get => _currentSite; set => _currentSite = value; }
        public Codeplug.System CurrentSystem { get => _currentSystem; set => _currentSystem = value; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Current_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            //MessageBox.Show($"Unhandled exception: {e.Exception.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }

        /// <summary>
        /// Helper to load the codeplug
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Handle PTT down
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void PTTButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_isPttCooldown || !_powerOn) return;

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
                        Site = CurrentSite
                    }
                };
                _webSocketHandler.SendMessage(request);
                StartPttCooldown();
            }
        }

        /// <summary>
        /// Start PTT cooldown timer
        /// </summary>
        private void StartPttCooldown()
        {
            _isPttCooldown = true;
            _pttCooldownTimer.Start();
        }

        /// <summary>
        /// PTT Cooldown timer tick
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PttCooldownTimer_Tick(object sender, EventArgs e)
        {
            _isPttCooldown = false;
            _pttCooldownTimer.Stop();
        }

        /// <summary>
        /// PTT Watchdog timer tick
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PttWatchdogTimer_Tick(object sender, EventArgs e)
        {
            _pttState = (NativeMethods.GetAsyncKeyState(VK_PTT) & 0x8000) != 0;

            if (_pttState)
            {
                _isPttActive = true;
            }
            else if (_isPttActive)
            {
                _isPttActive = false;
                PTTButton_MouseUp(null, null);
            }
        }

        /// <summary>
        /// Kill current master connection
        /// </summary>
        public void KillMasterConnection()
        {
            _webSocketHandler.Disconnect();
            _isInRange = false;
        }

        /// <summary>
        /// Create a master connection
        /// </summary>
        /// <param name="system"></param>
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

        /// <summary>
        /// Helper to send unit registration to master
        /// </summary>
        public void SendUnitRegistrationRequest()
        {
            var request = new
            {
                type = (int)PacketType.U_REG_REQ,
                data = new U_REG_REQ
                {
                    SrcId = _myRid,
                    SysId = "",
                    Wacn = "",
                    Site = CurrentSite
                }
            };
            _webSocketHandler.SendMessage(request);
        }

        /// <summary>
        /// Helper to send grp aff to master
        /// </summary>
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
                    SysId = "",
                    Site = CurrentSite
                }
            };
            _webSocketHandler.SendMessage(request);
        }

        /// <summary>
        /// Helper to send unit de registration to master
        /// </summary>
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
                    SysId = "",
                    Site = CurrentSite
                }
            };
            _webSocketHandler.SendMessage(request);
        }

        /// <summary>
        /// Helper to send emergency alarm req to master
        /// </summary>
        private void SendEmergencyAlarmRequest()
        {
            if (!_webSocketHandler.IsConnected || !_powerOn || !_isRegistered) return;

            var request = new
            {
                type = (int)PacketType.EMRG_ALRM_REQ,
                data = new EMRG_ALRM_REQ
                {
                    SrcId = _myRid,
                    DstId = _currentTgid,
                    Site = CurrentSite
                }
            };
            _webSocketHandler.SendMessage(request);
        }

        /// <summary>
        /// Helper to send call alert req to master
        /// </summary>
        private void SendCallAlertRequest()
        {
            if (!_webSocketHandler.IsConnected || !_powerOn || !_isRegistered) return;

            var request = new
            {
                type = (int)PacketType.CALL_ALRT_REQ,
                data = new CALL_ALRT_REQ
                {
                    SrcId = _myRid,
                    DstId = txt_Line2.Text,
                    Site = CurrentSite
                }
            };
            _webSocketHandler.SendMessage(request);
        }

        /// <summary>
        /// Handle PTT Up
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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
                        Channel = _currentChannel,
                        Site = CurrentSite
                    }
                };

                _currentChannel = string.Empty;
                Dispatcher.Invoke(() => txt_Line3.Text = "");
                _isReceiving = false;
                Dispatcher.Invoke(() => SetRssiSource("RSSI_COLOR_4.png"));

                _webSocketHandler.SendMessage(request);
                _waveIn.StopRecording();
            }
        }

        /// <summary>
        /// Callback for recording stop
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _waveIn_RecordingStopped(object sender, StoppedEventArgs e)
        {
            _isRecording = false;
        }

        /// <summary>
        /// Callback for audio available
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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
                        Frequency = _currentChannel,
                    },
                    site = CurrentSite
                };
                _webSocketHandler.SendMessage(audioData);
            }
        }

        /// <summary>
        /// Handle unit reg
        /// </summary>
        /// <param name="response"></param>
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

        /// <summary>
        /// Handle unit de reg response
        /// </summary>
        /// <param name="response"></param>
        private void HandleUnitDeRegistrationResponse(U_DE_REG_RSP response)
        {
            _deregistrationCompletionSource?.SetResult(true);
        }

        /// <summary>
        /// Handle grp aff response
        /// </summary>
        /// <param name="response"></param>
        private void HandleGroupAffiliationResponse(GRP_AFF_RSP response)
        {
            Dispatcher.Invoke(() => SetRssiSource("RSSI_COLOR_4.png"));
        }

        /// <summary>
        /// Handle grp vch release
        /// </summary>
        /// <param name="response"></param>
        private async void HandleVoiceChannelRelease(GRP_VCH_RLS response)
        {
            if (!_powerOn || !_isRegistered || (response.DstId != _currentTgid) || response.SrcId == _myRid) return;

            _currentChannel = string.Empty;
            Dispatcher.Invoke(() => txt_Line3.Text = "");
            Dispatcher.Invoke(() => SetRssiSource(""));
            _isReceiving = false;
            await Task.Delay(250);
            Dispatcher.Invoke(() => SetRssiSource("RSSI_COLOR_4.png"));
        }

        /// <summary>
        /// Handle voice channel response
        /// </summary>
        /// <param name="response"></param>
        private async void HandleVoiceChannelResponse(GRP_VCH_RSP response)
        {
            if (!_powerOn || !_isRegistered) return;

            if (response.Status == (int)ResponseType.GRANT && response.SrcId == _myRid && response.DstId == _currentTgid)
            {
                _currentChannel = response.Channel;
                await Task.Delay(100);
                Dispatcher.Invoke(() => SetRssiSource("TX_RSSI.png"));

                AudioPlayer.PlaySound(Properties.Resources.trunking_tpt);

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
                Dispatcher.Invoke(() => SetRssiSource("RSSI_COLOR_4.png"));
                Console.WriteLine("Channel request denied.");
            }
            else if (response.DstId == _currentTgid && response.SrcId != _myRid)
            {
                _currentChannel = response.Channel;
                Dispatcher.Invoke(() => txt_Line3.Text = $"ID: {response.SrcId}");
                Dispatcher.Invoke(() => SetRssiSource("RX_COLOR.png"));
            }
        }

        /// <summary>
        /// Handle emerg
        /// </summary>
        /// <param name="response"></param>
        private void HandleEmergencyAlarmResponse(EMRG_ALRM_RSP response)
        {
            Dispatcher.Invoke(() => SetLine3Text($"EM: {response.SrcId}"));
        }

        /// <summary>
        /// Handle call alert
        /// </summary>
        /// <param name="response"></param>
        private void HandleCallAlert(CALL_ALRT response)
        {
            if (response.DstId != _myRid) return;

            AudioPlayer.PlaySound(Properties.Resources.call_alert);
            Dispatcher.Invoke(() => SetLine3Text($"Page rcv: {response.SrcId}"));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                PTTButton_MouseDown(sender, null);
                e.Handled = true;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainWindow_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                PTTButton_MouseUp(sender, null);
                e.Handled = true;
            }
        }

        /// <summary>
        /// Cleanup
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Window_Closing(object sender, CancelEventArgs e)
        {
            if (_webSocketHandler != null)
            {
                _deregistrationCompletionSource = new TaskCompletionSource<bool>();
                SendUnitDeRegistrationRequest();
                await _deregistrationCompletionSource.Task;
            }
        }

        /// <summary>
        /// Helper to enter the page menu
        /// </summary>
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

        /// <summary>
        /// Helper to exit the page menu
        /// </summary>
        private void ExitPageMenu()
        {
            ClearFields(true);
            txt_Line2.IsReadOnly = true;
            _isInMenu = false;
            SetupSoftButtons();
            _radioDisplayUpdater.UpdateDisplay(_codeplug, _currentZoneIndex, _currentChannelIndex, false, false, false);
        }

        /// <summary>
        /// Setup the soft buttons
        /// </summary>
        private void SetupSoftButtons()
        {
            txt_SoftMenu1.Text = "PAGE";
            txt_SoftMenu4.Text = string.Empty;
        }

        /// <summary>
        /// Helper to clear most fields
        /// </summary>
        /// <param name="forMenu"></param>
        private void ClearFields(bool forMenu = false)
        {
            if (_isInMenu && !forMenu) return;

            SetLine1Text("");
            SetLine2Text("");
            SetLine3Text("");

            if (!forMenu)
                SetRssiSource("");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="text"></param>
        /// <param name="forMenu"></param>
        public void SetLine1Text(string text, bool forMenu = false)
        {
            if (_isInMenu && !forMenu) return;

            txt_Line1.Text = text;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="text"></param>
        /// <param name="forMenu"></param>
        public void SetLine2Text(string text, bool forMenu = false)
        {
            if (_isInMenu && !forMenu) return;

            txt_Line2.Text = text;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="text"></param>
        /// <param name="forMenu"></param>
        public void SetLine3Text(string text, bool forMenu = false)
        {
            if (_isInMenu && !forMenu) return;

            txt_Line3.Text = text;
        }

        /// <summary>
        /// Helper to set RSSI icon source
        /// </summary>
        /// <param name="name"></param>
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

        /// <summary>
        /// Helper to play audio data
        /// </summary>
        /// <param name="audioData"></param>
        /// <param name="voiceChannel"></param>
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

        /// <summary>
        /// Power Button clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_Power_Click(object sender, RoutedEventArgs e)
        {
            if (!_powerOn)
                TogglePowerOn();
            else
                PowerOff();

            _powerOn = !_powerOn;
        }

        /// <summary>
        /// Emerg button clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_Emerg_Click(object sender, RoutedEventArgs e)
        {
            SendEmergencyAlarmRequest();
        }

        /// <summary>
        /// Helper to power on radio
        /// </summary>
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

            _proc = HookCallback;
            _hookID = SetHook(_proc);
            
            _radioDisplayUpdater.UpdateDisplay(_codeplug, _currentZoneIndex, _currentChannelIndex, true, true);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_Emerg_MouseDown(object sender, MouseButtonEventArgs e)
        {
            SetLine3Text(null);
            e.Handled = true;
        }

        /// <summary>
        /// Helper to power off the radio
        /// </summary>
        private void PowerOff()
        {
            SendUnitDeRegistrationRequest();
            ClearFields();
            txt_SoftMenu1.Text = string.Empty;
            txt_SoftMenu4.Text = string.Empty;
            _isRegistered = false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_ZoneUp_Click(object sender, RoutedEventArgs e)
        {
            ChangeZone(1);
        }

        private void btn_ZoneDown_Click(object sender, RoutedEventArgs e)
        {
            ChangeZone(-1);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_ChangeChannel_Click(object sender, RoutedEventArgs e)
        {
            ChangeChannel();
        }

        /// <summary>
        /// Helper to change zones
        /// </summary>
        /// <param name="direction"></param>
        private void ChangeZone(int direction)
        {
            if (!_powerOn || _codeplug == null) return;

            _currentZoneIndex += direction;
            if (_currentZoneIndex < 0) _currentZoneIndex = _codeplug.Zones.Count - 1;
            else if (_currentZoneIndex >= _codeplug.Zones.Count) _currentZoneIndex = 0;

            _currentChannelIndex = 0;
            _radioDisplayUpdater.UpdateDisplay(_codeplug, _currentZoneIndex, _currentChannelIndex, true, true);
        }

        /// <summary>
        /// Helper to change channels
        /// </summary>
        private void ChangeChannel()
        {
            if (!_powerOn || _codeplug == null) return;

            var zone = _codeplug.Zones[_currentZoneIndex];
            _currentChannelIndex++;
            if (_currentChannelIndex >= zone.Channels.Count) _currentChannelIndex = 0;

            _radioDisplayUpdater.UpdateDisplay(_codeplug, _currentZoneIndex, _currentChannelIndex, true, false);
        }

        /// <summary>
        /// Handle Master connection
        /// </summary>
        private void HandleConnectionOpen()
        {
            if (_powerOn)
                Dispatcher.Invoke(() => SetRssiSource("RSSI_COLOR_4.png"));

            _isInRange = true;
            _reconnectTimer.Stop();
        }

        /// <summary>
        /// Handle Master connecion close
        /// </summary>
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

        /// <summary>
        /// Master reconnect timer tick
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ReconnectTimer_Tick(object sender, EventArgs e)
        {
            if (!_webSocketHandler.IsConnected && _currentSystem != null && _powerOn)
            {
                KillMasterConnection();
                MasterConnection(_currentSystem);
                SendUnitRegistrationRequest();
                SendGroupAffiliationRequest();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_SoftMenu1_Click(object sender, RoutedEventArgs e)
        {
            if (!_isInMenu)
                EnterPageMenu();
            else
                SendCallAlertRequest();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_SoftMenu4_Click(object sender, RoutedEventArgs e)
        {
            if (_isInMenu)
                ExitPageMenu();
            else
                BeepGenerator.Bonk();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hwnd"></param>
        /// <param name="msg"></param>
        /// <param name="wParam"></param>
        /// <param name="lParam"></param>
        /// <param name="handled"></param>
        /// <returns></returns>
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_MOUSEACTIVATE = 0x0021;
            if (msg == WM_MOUSEACTIVATE)
            {
                handled = true;
                return new IntPtr(MA_NOACTIVATEANDEAT);
            }

            return IntPtr.Zero;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
            HwndSource.FromHwnd(hwnd)?.AddHook(new HwndSourceHook(WndProc));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="proc"></param>
        /// <returns></returns>
        private IntPtr SetHook(NativeMethods.LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return NativeMethods.SetWindowsHookEx(NativeMethods.WH_KEYBOARD_LL, proc, NativeMethods.GetModuleHandle(curModule.ModuleName), 0);
            }
        }
                
        /// <summary>
        /// 
        /// </summary>
        /// <param name="nCode"></param>
        /// <param name="wParam"></param>
        /// <param name="lParam"></param>
        /// <returns></returns>
        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            try
            {       
                if (nCode >= 0)
                {
                    int vkCode = Marshal.ReadInt32(lParam);
                    Console.WriteLine(vkCode);             

                    if (wParam == (IntPtr)NativeMethods.WM_KEYDOWN)    
                    {
                        OnGlobalKeyDown(vkCode);
                    }
                    else if (wParam == (IntPtr)NativeMethods.WM_KEYUP)
                    {
                        OnGlobalKeyUp(vkCode);
                    }
                }

                return NativeMethods.CallNextHookEx(_hookID, nCode, wParam, lParam); // TODO: Maybe a crash issue to look at
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return nint.Zero;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="vkCode"></param>
        private void OnGlobalKeyDown(int vkCode)
        {
            if (vkCode == 33)
            {
                PTTButton_MouseDown(null, null);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="vkCode"></param>
        private void OnGlobalKeyUp(int vkCode)
        {
            if (vkCode == 33)
            {
                PTTButton_MouseUp(null, null);
            }
        }

        private const int SWP_NOMOVE = 0x0002;
        private const int SWP_NOSIZE = 0x0001;
        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private const int MA_NOACTIVATEANDEAT = 4;

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_Minimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_FocusRadioToggle_Click(object sender, RoutedEventArgs e)
        {
/*            if (_isFocused)
            {
                IntPtr hwnd = new WindowInteropHelper(this).Handle;
                SetWindowPos(hwnd, HWND_BOTTOM, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
                _isFocused = false;
            }
            else
            {
                this.Activate();
                _isFocused = true;
            }*/
        }
    }
}