using WhackerLinkCommonLib.Interfaces;
using WhackerLinkCommonLib.Models.Radio;
using WhackerLinkCommonLib.Utils;

#nullable disable

namespace WhackerLinkCommonLib.UI
{
    public class RadioDisplayUpdater
    {
        private readonly IRadioDisplay _radioDisplay;

        public RadioDisplayUpdater(IRadioDisplay radioDisplay)
        {
            _radioDisplay = radioDisplay;
        }

        public async void UpdateDisplay(Codeplug codeplug, int currentZoneIndex, int currentChannelIndex, bool systemChange = true, bool zoneChange = false, bool tts =  true)
        {
            if (codeplug != null && codeplug.Zones.Count > 0)
            {
                var zone = codeplug.Zones[currentZoneIndex];

                if (zone.Channels.Count > 0)
                {
                    var channel = zone.Channels[currentChannelIndex];

                    _radioDisplay.SetLine1Text(zone.Name);
                    _radioDisplay.SetLine2Text(channel.Name);

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                    if (tts)
                    {
                        Task.Run(() =>
                        {
                            if (zoneChange)
                                Util.SpeakText(zone.Name);
                            Util.SpeakText(channel.Name);
                        });
                    }
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

                    _radioDisplay.CurrentTgid = channel.Tgid;
                    var newSystem = codeplug.GetSystemForChannel(channel);

                    if (!_radioDisplay.PowerOn || (_radioDisplay.CurrentSystem == null || _radioDisplay.CurrentSystem.Name != newSystem.Name))
                    {
                        _radioDisplay.CurrentSystem = newSystem;
                        _radioDisplay.MyRid = newSystem.Rid;

                        _radioDisplay.KillMasterConnection();
                        _radioDisplay.MasterConnection(newSystem);

                        if (systemChange)
                        {
                            if (!_radioDisplay.IsInRange)
                            {
                                await Task.Delay(200);
                                _radioDisplay.SetRssiSource("RSSI_COLOR_0.png");
                                _radioDisplay.SetLine3Text("Out of Range");
                            } else
                            {
                                _radioDisplay.SetRssiSource("TX_RSSI.png");
                                await Task.Delay(150);
                                _radioDisplay.SetRssiSource("");
                                await Task.Delay(200);
                                _radioDisplay.SetRssiSource("TX_RSSI.png");
                                await Task.Delay(250);
                                _radioDisplay.SetRssiSource("");
                                await Task.Delay(350);
                            }

                            _radioDisplay.SendUnitRegistrationRequest();
                        }
                    }
                    else
                    {
                        if (!_radioDisplay.IsInRange)
                        {
                            await Task.Delay(200);
                            _radioDisplay.SetRssiSource("RSSI_COLOR_0.png");
                            _radioDisplay.SetLine3Text("Out of Range");
                        } else
                        {
                            _radioDisplay.SetRssiSource("TX_RSSI.png");
                            await Task.Delay(200);
                            _radioDisplay.SetRssiSource(null);
                        }
                    }

                    _radioDisplay.SendGroupAffiliationRequest();
                }
            }
        }
    }
}