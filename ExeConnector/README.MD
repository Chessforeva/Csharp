
<b>ExeConnector</b> is a small webserver that lets connect chess engine or telnet terminal putty to the web. For example, can fetch on Google Colab, or make a web-server based on Windows that plays strong Stockfish chess. The .cs sources are very old but still good for MS Studio 2022.

The ip.dat file holds IP to use for server. Can left 127.0.0.1 that is localhost, or can set current IP of the PC.
It is feature not a bug :)

Anyway, can obtain IP by

  ```sh

 private string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            throw new Exception("No network adapters with an IPv4 address in the system!");
        }

  ```
