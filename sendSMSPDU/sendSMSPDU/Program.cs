using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


// добавим
using System.IO.Ports;


namespace sendSMSPDU
{
    class Program
    {
        static SerialPort port;

        static void Main(string[] args)
        {
            port = new SerialPort();

            Console.WriteLine("Отправка сообщения СМС");

            OpenPort();
            bool result;
            result = sendSMS("Привет!!!", "+375123456789");

            if (result == true)
            {
                Console.WriteLine("Сообщение отправлено успешно");
            }
            else
            {
                Console.WriteLine("Произошла ошибка при отправке");
            }
            Console.ReadLine();

            port.Close();
        }



        private static bool sendSMS(string textsms, string telnumber)
        {
            if (!port.IsOpen) return false;

            try
            {
                System.Threading.Thread.Sleep(500);
                port.WriteLine("AT\r\n"); // означает "Внимание!" для модема 
                System.Threading.Thread.Sleep(500);

                port.Write("AT+CMGF=0\r\n"); // устанавливается цифровой режим PDU для отправки сообщений
                System.Threading.Thread.Sleep(500);
            }
            catch
            {
                return false;
            }

            try
            {
                telnumber = telnumber.Replace("-", "").Replace(" ", "").Replace("+", "");

                // 01 это PDU Type или иногда называется SMS-SUBMIT. 01 означает, что сообщение передаваемое, а не получаемое 
                // цифры 00 это TP-Message-Reference означают, что телефон/модем может установить количество успешных сообщений автоматически
                // telnumber.Length.ToString("X2") выдаст нам длинну номера в 16-ричном формате
                // 91 означает, что используется международный формат номера телефона
                telnumber = "01" + "00" + telnumber.Length.ToString("X2") + "91" + EncodePhoneNumber(telnumber);

                textsms = StringToUCS2(textsms);
                // 00 означает, что формат сообщения неявный. Это идентификатор протокола. Другие варианты телекс, телефакс, голосовое сообщение и т.п.
                // 08 означает формат UCS2 - 2 байта на символ. Он проще, так что рассмотрим его.
                // если вместо 08 указать 18, то сообщение не будет сохранено на телефоне. Получится flash сообщение
                string leninByte = (textsms.Length / 2).ToString("X2");
                textsms = telnumber + "00" + "08" + leninByte + textsms;

                // посылаем команду с длинной сообщения - количество октет в десятичной системе. то есть делим на два количество символов в сообщении
                // если октет неполный, то получится в результате дробное число. это дробное число округляем до большего
                double lenMes = textsms.Length / 2;
                port.Write("AT+CMGS=" + (Math.Ceiling(lenMes)).ToString() + "\r\n");
                System.Threading.Thread.Sleep(500);

                // номер sms-центра мы не указываем, считая, что практически во всех SIM картах он уже прописан
                // для того, чтобы было понятно, что этот номер мы не указали добавляем к нашему сообщению в начало 2 нуля
                // добавляем именно ПОСЛЕ того, как подсчитали длинну сообщения
                textsms = "00" + textsms;

                port.Write(textsms + char.ConvertFromUtf32(26) + "\r\n");
                System.Threading.Thread.Sleep(500);
            }
            catch
            {
                return false;
            }

            try
            {
                string recievedData;
                recievedData = port.ReadExisting();

                if (recievedData.Contains("ERROR"))
                {
                    return false;
                }

            }
            catch { }

            return true;
        }




        private static void OpenPort()
        {
            string[] pn;
            pn = SerialPort.GetPortNames();

            port.BaudRate = 2400; // еще варианты 4800, 9600, 28800 или 56000
            port.DataBits = 7; // еще варианты 8, 9

            port.StopBits = StopBits.One; // еще варианты StopBits.Two StopBits.None или StopBits.OnePointFive         
            port.Parity = Parity.Odd; // еще варианты Parity.Even Parity.Mark Parity.None или Parity.Space

            port.ReadTimeout = 500; // еще варианты 1000, 2500 или 5000 (больше уже не стоит)
            port.WriteTimeout = 500; // еще варианты 1000, 2500 или 5000 (больше уже не стоит)

            //port.Handshake = Handshake.RequestToSend;
            //port.DtrEnable = true;
            //port.RtsEnable = true;
            //port.NewLine = Environment.NewLine;

            port.Encoding = Encoding.GetEncoding("windows-1251");

            port.PortName = "COM5";

            // незамысловатая конструкция для открытия порта
            if (port.IsOpen)
                port.Close();
            try
            {
                port.Open();
            }
            catch { }

        }



        // перекодирование номера телефона для формата PDU
        public static string EncodePhoneNumber(string PhoneNumber)
        {
            string result = "";
            if ((PhoneNumber.Length % 2) > 0) PhoneNumber += "F";

            int i = 0;
            while (i < PhoneNumber.Length)
            {
                result += PhoneNumber[i + 1].ToString() + PhoneNumber[i].ToString();
                i += 2;
            }
            return result.Trim();
        }


        // перекодирование текста смс в UCS2 
        public static string StringToUCS2(string str)
        {
            UnicodeEncoding ue = new UnicodeEncoding();
            byte[] ucs2 = ue.GetBytes(str);

            int i = 0;
            while (i < ucs2.Length)
            {
                byte b = ucs2[i + 1];
                ucs2[i + 1] = ucs2[i];
                ucs2[i] = b;
                i += 2;
            }
            return BitConverter.ToString(ucs2).Replace("-", "");
        }


    }
}

