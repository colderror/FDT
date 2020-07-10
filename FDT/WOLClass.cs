using System;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace FDT
{
    public class WOLClass
    {
        public void WakeOnLan()
        {
            var macAddress = "B4-B6-86-29-60-78";                      // Our device MAC address
            macAddress = Regex.Replace(macAddress, "[-|:]", "");       // Remove any semicolons or minus characters present in our MAC address

            var sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp)
            {
                EnableBroadcast = true
            };

            int payloadIndex = 0;

            /* The magic packet is a broadcast frame containing anywhere within its payload 6 bytes of all 255 (FF FF FF FF FF FF in hexadecimal), followed by sixteen repetitions of the target computer's 48-bit MAC address, for a total of 102 bytes. */
            byte[] payload = new byte[1024];    // Our packet that we will be broadcasting

            // Add 6 bytes with value 255 (FF) in our payload
            for (int i = 0; i < 6; i++)
            {
                payload[payloadIndex] = 255;
                payloadIndex++;
            }

            // Repeat the device MAC address sixteen times
            for (int j = 0; j < 16; j++)
            {
                for (int k = 0; k < macAddress.Length; k += 2)
                {
                    var s = macAddress.Substring(k, 2);
                    payload[payloadIndex] = byte.Parse(s, NumberStyles.HexNumber);
                    payloadIndex++;
                }
            }

            sock.SendTo(payload, new IPEndPoint(IPAddress.Parse("255.255.255.255"), 0));  // Broadcast our packet
            sock.Close(10000);
        }
        //public WOLClass() : base()
        //{ }
        //this is needed to send broadcast packet
        //    public void SetClientToBrodcastMode()
        //    {
        //        if (this.Active)
        //            this.Client.SetSocketOption(SocketOptionLevel.Socket,
        //                                      SocketOptionName.Broadcast, 0);
        //    }

        //    private void WakeFunction(string MAC_ADDRESS = "B4-B6-86-29-60-78")
        //    {
        //        WOLClass client = new WOLClass();
        //        client.Connect(new
        //           IPAddress(0xffffffff),  //255.255.255.255  i.e broadcast
        //           0x2fff); // port=12287 let's use this one 
        //        client.SetClientToBrodcastMode();
        //        //set sending bites
        //        int counter = 0;
        //        //buffer to be send
        //        byte[] bytes = new byte[1024];   // more than enough :-)
        //                                         //first 6 bytes should be 0xFF
        //        for (int y = 0; y < 6; y++)
        //            bytes[counter++] = 0xFF;
        //        //now repeate MAC 16 times
        //        for (int y = 0; y < 16; y++)
        //        {
        //            int i = 0;
        //            for (int z = 0; z < 6; z++)
        //            {
        //                bytes[counter++] =
        //                    byte.Parse(MAC_ADDRESS.Substring(i, 2),
        //                    NumberStyles.HexNumber);
        //                i += 2;
        //            }
        //        }

        //        //now send wake up packet
        //        int reterned_value = client.Send(bytes, 1024);
        //    }
        //}
    }
}
