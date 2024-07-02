using System.Threading.Tasks;
using WhackerLinkCommonLib.Interfaces;
using WhackerLinkCommonLib.Models.Radio;

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

        public async void UpdateDisplay(Codeplug codeplug, int currentZoneIndex, int currentChannelIndex, bool systemChange = true)
        {
            if (codeplug != null && codeplug.Zones.Count > 0)
            {
                var zone = codeplug.Zones[currentZoneIndex];
                _radioDisplay.SetLine1Text(zone.Name);

                if (zone.Channels.Count > 0)
                {
                    var channel = zone.Channels[currentChannelIndex];
                    _radioDisplay.SetLine2Text(channel.Name);
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
                            _radioDisplay.SetRssiSource("TX_RSSI.png");
                            await Task.Delay(150);
                            _radioDisplay.SetRssiSource("");
                            await Task.Delay(200);
                            _radioDisplay.SetRssiSource("TX_RSSI.png");
                            await Task.Delay(250);
                            _radioDisplay.SetRssiSource("");
                            await Task.Delay(350);
                            _radioDisplay.SendUnitRegistrationRequest();
                        }
                    }
                    else
                    {
                        _radioDisplay.SetRssiSource("TX_RSSI.png");
                        await Task.Delay(200);
                        _radioDisplay.SetRssiSource(null);
                    }

                    _radioDisplay.SendGroupAffiliationRequest();
                }
            }
        }
    }
}
