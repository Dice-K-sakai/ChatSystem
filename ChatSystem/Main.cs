using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using ChatSystem;

namespace ChatSystem
{
    class main
    {
        static ChatSystem chatSystem;
        const Int32 portNo = 11000;
        const string EOF = "<EOF>";
        static readonly int maxLength = 200 + EOF.Length;
        static ChatSystem.ConnectMode connectMode;

        static string user_name = "";

        static void Main(string[] args)
        {
            chatSystem = new ChatSystem(maxLength);
            Console.WriteLine($"このPCのホスト名は {chatSystem.hostName}です。");

            while (true)
            {
                Console.Write("名前を入力してください。:");
                user_name = Console.ReadLine();

                if (user_name != "")
                {
                    user_name += ":";
                    break;
                }
                else
                {
                    Console.WriteLine("名前が検出できませんでした。もう一度入力してください。\n");
                }

            }

            connectMode = SelectMode();
            InChat();

        }

        static ChatSystem.ConnectMode SelectMode()
        {
            ChatSystem.ConnectMode connectMode = ChatSystem.ConnectMode.host;

            while (true)
            {
                Console.Write("モードを選択してください。[ 0か1を入力 ]\n{ 0 : Host , 1 : Client } : ");
                int select = int.Parse(Console.ReadLine());

                switch (select)
                {
                    case 0:

                        //Host
                        Console.WriteLine("ホストモードで起動します。");
                        InitializeHost();
                        connectMode = ChatSystem.ConnectMode.host;
                        break;

                    case 1:

                        //Client
                        Console.WriteLine("クライアントモードで起動します。");
                        InitializeClient();
                        connectMode = ChatSystem.ConnectMode.client;
                        break;

                    default:

                        Console.WriteLine("入力が未定義でした。もう一度入力してください。\n");
                        break;
                }

                if (select == 0 || select == 1)
                {
                    break;
                }
            }

            return connectMode;
        }

        static void InitializeHost()
        {
            IPHostEntry ipHostInfo = Dns.GetHostEntry(chatSystem.hostName);
            
            foreach (var addresslist in ipHostInfo.AddressList)
            {
                Console.WriteLine($"自分のアドレスが見つかりました:{addresslist.ToString()}");
            }

            int address_Select = 0;
            while (true)
            {
                Console.Write($"\n公開するアドレスを選択してください。(0 から {ipHostInfo.AddressList.Length - 1}):");

                address_Select = int.Parse(Console.ReadLine());

                if (address_Select >= 0 && address_Select <= ipHostInfo.AddressList.Length - 1)
                {
                    break;
                }
                else
                {
                    Console.WriteLine("例外が入力されました。もう一度入力してください。");
                }
            }

            Console.WriteLine("クライアント接続待ち…");

            IPAddress ipAddress = ipHostInfo.AddressList[address_Select];
            ChatSystem.EResult re = chatSystem.InitializeHost(ipAddress, portNo);

            Console.WriteLine("\n\n\n");//改行

            if (re != ChatSystem.EResult.success)
            {
                Console.WriteLine($"初期化に失敗しました。\nエラー内容 = {re.ToString()}");
            }

        }

        static void InitializeClient()
        {
            Console.Write("接続するIPアドレスを入力してください。:");
            var ipAddress = IPAddress.Parse(Console.ReadLine());
            ChatSystem.EResult re = chatSystem.InitializeClient(ipAddress, 11000);
            
            if (re == ChatSystem.EResult.success)
            {
                Console.WriteLine($"接続されたホスト。:{ipAddress.ToString()} \n\n\n");
            }
            else
            {
                Console.WriteLine($"ホストへの接続に失敗しました。\nエラー内容 ={chatSystem.resultMessage}");
            }
        }

        static void InChat()
        {
            ChatSystem.Buffer buffer = new ChatSystem.Buffer(maxLength);
            bool turn = (connectMode == ChatSystem.ConnectMode.host);

            Console.WriteLine("チャット開始");

            while (true)
            {
                if (turn)
                {   
                    // 受信
                    buffer = new ChatSystem.Buffer(maxLength);
                    ChatSystem.EResult re = chatSystem.Receive(buffer);

                    if (re == ChatSystem.EResult.success)
                    {
                        string received = Encoding.UTF8.GetString(buffer.content).Replace(EOF, "");
                        int l = received.Length;

                        if (received[0] != '\0')
                        {   
                            // 正常にメッセージを受信
                            Console.WriteLine($"{received}");
                        }
                        else
                        {   
                            // 正常に終了を受信
                            Console.WriteLine("相手から終了を受信");
                            break;
                        }
                    }
                    else
                    {   
                        //　受信エラー
                        Console.WriteLine($"受信エラー：{chatSystem.resultMessage} ");
                        break;
                    }
                }
                else
                {   
                    // 送信
                    Console.Write(user_name);
                    string inputSt = Console.ReadLine();

                    if (inputSt.Length > maxLength)
                    {
                        inputSt = inputSt.Substring(0, maxLength - EOF.Length - user_name.Length - 1);
                    }

                    string input = user_name + inputSt + EOF;

                    if (inputSt == "") 
                    {
                        input = "\0";
                    }

                    buffer.content = Encoding.UTF8.GetBytes(input);
                    buffer.length = buffer.content.Length;
                    ChatSystem.EResult re = chatSystem.Send(buffer);
                    
                    if (re != ChatSystem.EResult.success)
                    {
                        Console.WriteLine($"送信エラー：{re.ToString()} エラーコード : {chatSystem.resultMessage}");
                        break;
                    }

                    if (inputSt == "\0")
                    {
                        break;
                    }
                }

                turn = !turn;
            }

            chatSystem.ShutDownColse();
        }
    }
}