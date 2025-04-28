/*
* WhackerLink - WhackerLinkServer
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

#if AMBEVOCODE
namespace WhackerLinkServer
{
    public class AmbeVocoderManager
    {
        private static readonly object lockObj = new object();
        private AmbeVocoder vocoder;

        public AmbeVocoderManager(bool fullRate = true)
        {
            vocoder = new AmbeVocoder(fullRate);
        }

        public int Decode(byte[] codeword, out short[] samples)
        {
            lock (lockObj)
            {
                return vocoder.decode(codeword, out samples);
            }
        }

        public void Encode(short[] samples, out byte[] codeword)
        {
            lock (lockObj)
            {
                vocoder.encode(samples, out codeword);
            }
        }
    }

}
#endif