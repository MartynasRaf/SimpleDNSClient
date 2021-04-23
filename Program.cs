using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace DNS_Client
{
    // DNS Client is also called a resolver.
    // DNS Client is a client machine configured to send name resolution queries to a DNS server.

    class Program
    {



        static void Main(string[] args)
        {

            Socket sock = new Socket(SocketType.Dgram, ProtocolType.Udp);

            IPEndPoint ep = new IPEndPoint(IPAddress.Parse("8.8.8.8"), 53);
            sock.Connect(ep);

            Console.WriteLine("Enter domain to get IP adress from Google public DNS (8.8.8.8)");

            String host1 = Console.ReadLine();
            byte[] hostnameLength = new byte[1];
            byte[] hostdomainLength = new byte[1];


            byte[] tranactionID1 = { 0x46, 0x62 };
            byte[] queryType1 = { 0x01, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
            byte[] hostname = System.Text.ASCIIEncoding.Default.GetBytes(host1.Split('.')[0]);
            hostnameLength[0] = (byte)hostname.Length;
            byte[] hostdomain = System.Text.ASCIIEncoding.Default.GetBytes(host1.Split('.')[1]);
            hostdomainLength[0] = (byte)hostdomain.Length;
            byte[] queryEnd = { 0x00, 0x00, 0x01, 0x00, 0x01 };
            byte[] dnsQueryString = tranactionID1.Concat(queryType1).Concat(hostnameLength).Concat(hostname).Concat(hostdomainLength).Concat(hostdomain).Concat(queryEnd).ToArray();

            sock.Send(dnsQueryString);

            byte[] rBuffer = new byte[1000];

            int receivedLength = sock.Receive(rBuffer);


            var transId = (ushort)BitConverter.ToInt16(new[] { rBuffer[1], rBuffer[0] }, 0);
            var queCount = (ushort)BitConverter.ToInt16(new[] { rBuffer[5], rBuffer[4] }, 0);
            var ansCount = (ushort)BitConverter.ToInt16(new[] { rBuffer[7], rBuffer[6] }, 0);
            var authCount = (ushort)BitConverter.ToInt16(new[] { rBuffer[9], rBuffer[8] }, 0);
            var addCount = (ushort)BitConverter.ToInt16(new[] { rBuffer[11], rBuffer[10] }, 0);


            // Header read, now on to handling questions

            int byteCount = 12;


            Question[] questions = new Question[queCount];


            for (int i=0;i< queCount; i++)
            {
                
                // Read Name
                while (true)
                {

                    int stringLength = rBuffer[byteCount];
                    byteCount++;

                    if (stringLength == 0) { 
                        
                        if (questions[i].qName[questions[i].qName.Length - 1] == '.')
                        {
                            questions[i].qName = new string(questions[i].qName.Take(questions[i].qName.Length - 1).ToArray());
                        }

                        break; 
                    }

                    byte[] tempName = new byte[stringLength];

                    for(int k=0;k< stringLength; k++)
                    {
                        tempName[k] = rBuffer[byteCount];
                        byteCount++;
                    }

                    questions[i].qName += Encoding.ASCII.GetString(tempName) + '.';

                }

                // Name read now read Type

                questions[i].qType = rBuffer[byteCount] + rBuffer[byteCount + 1];
                byteCount += 2;

                questions[i].qClass = rBuffer[byteCount] + rBuffer[byteCount + 1];
                byteCount += 2;

            }

            Answer[] answers = new Answer[ansCount];

           
                       
            for(int i =0; i< ansCount; i++)
            {

                // Skip reading Name, since it points to the Name given in question
                byteCount += 2;

                answers[i].aType = rBuffer[byteCount] + rBuffer[byteCount + 1];
                byteCount += 2;

                answers[i].aClass = rBuffer[byteCount] + rBuffer[byteCount + 1];
                byteCount += 2;

                answers[i].aTtl = BitConverter.ToInt32(rBuffer.Skip(byteCount).Take(4).Reverse().ToArray());
                byteCount += 4;

                answers[i].rdLength = BitConverter.ToInt16(rBuffer.Skip(byteCount).Take(2).Reverse().ToArray());
                byteCount += 2;

                answers[i].rData = rBuffer.Skip(byteCount).Take(answers[i].rdLength).ToArray();
                byteCount += answers[i].rdLength;

            }


            foreach(var a in answers)
            {
                // the canonical name for an alias
                if (a.aType == 5)
                {
                    string namePortion="";
                    for (int bytePosition = 0 ; bytePosition < a.rData.Length  ; )
                    {

                        int length = a.rData[bytePosition];
                        bytePosition++;

                        if (length == 0) continue;

                        namePortion += Encoding.ASCII.GetString(a.rData.Skip(bytePosition).Take(length).ToArray()) + ".";

                        bytePosition += length;
                    }

                    Console.WriteLine(new string(namePortion.Take(namePortion.Length - 1).ToArray()));

                }

                // Skip any answer that's not IP adress since it's irrelevant for this excercise
                if (a.aType == 1)
                {

                    // First byte tells the lenghth of data (Usually length of 4 since type 1 describes IP4 adresses)


                    string ipString = "";

                    foreach (var b in a.rData.ToArray())
                    {
                        int number = b;

                        ipString += number + ".";

                    }

                    Console.WriteLine(new string(ipString.Take(ipString.Length - 1).ToArray()));

                }
            }
            
         sock.Close();

        }

        struct Question
        {
            public string qName;

            public int qType;

            public int qClass;

        }

        struct Answer
        {

            public List<byte> aName;

            public int aType;

            public int aClass;

            public int aTtl;

            public int rdLength;

            public byte[] rData;

        }



    }
}
