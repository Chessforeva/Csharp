using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExeConnector
{
	using System;
	using System.IO;
	using System.Net;
	using System.Net.Sockets;
	using System.Text;
	using System.Threading;
	using System.Diagnostics;
	using System.Web;


	public class MyWebServer
	{
		Thread th;

		private TcpListener myListener;
		private String ip; // IP to listen
		private int port; // port to listen


		private String OUT_BUFFER = "";
		private String IN_BUFFER = "";

		Process process = null;

		public String CurPath = "";
		private String CmdExe = "";
		private String CmdArgs = "";

		StreamWriter WriteStream;
		//StreamReader ReadStream;


		//The constructor which make the TcpListener start listening on the
		//given port. It also calls a Thread on the method StartListen(). 
		public MyWebServer()
		{

			CurPath = Environment.CurrentDirectory.ToString();
			if (CurPath.Length > 0) CurPath += "\\";

			try
			{
				ip = GetIpNumber();             // ip.dat contains the right IP to communicate...
				port = GetPortNumber();         // port.dat contains the right port to communicate...
				CmdExe = GetCmdExe();           // Exe2Start.dat contains EXE relative filename to start process...
				CmdArgs = GetCmdArguments();    // Arguments.dat contains command line arguments...

				//start listing on the given port

				// IPAddress[] addrs = Dns.GetHostEntry(IPAddress.Parse("127.0.0.1")).AddressList;
				//foreach (IPAddress addr in addrs) CurrentIP = addr.ToString();


				// Create Local listener...
				myListener = new TcpListener(IPAddress.Parse(ip), port);
				myListener.Start();
				Console.WriteLine("Web Server & transit sockets are Running... Press ^C to Stop...");
				//start the thread which calls the method 'StartListen'
				th = new Thread(new ThreadStart(StartListen));
				th.Start();
				StartExe();           // Start Exe process...

			}
			catch (Exception e)
			{
				Console.WriteLine("An Exception Occurred while Listening :" + e.ToString());
			}
		}


		/// <summary>
		/// Returns The Default File Name
		/// Input : WebServerRoot Folder
		/// Output: Default File Name
		/// </summary>
		/// <param name="sMyWebServerRoot"></param>
		/// <returns></returns>
		public string GetTheDefaultFileName(string sLocalDirectory)
		{
			StreamReader sr;
			String sLine = "";

			try
			{
				//Open the default.dat to find out the list
				// of default file
				sr = new StreamReader("data\\Default.Dat");

				while ((sLine = sr.ReadLine()) != null)
				{
					//Look for the default file in the web server root folder
					if (File.Exists(sLocalDirectory + sLine) == true)
						break;
				}
			}
			catch (Exception e)
			{
				Console.WriteLine("An Exception Occurred : " + e.ToString());
			}
			if (File.Exists(sLocalDirectory + sLine) == true)
				return sLine;
			else
				return "";
		}



		/// <summary>
		/// This function takes FileName as Input and returns the mime type..
		/// </summary>
		/// <param name="sRequestedFile">To indentify the Mime Type</param>
		/// <returns>Mime Type</returns>
		public string GetMimeType(string sRequestedFile)
		{


			StreamReader sr;
			String sLine = "";
			String sMimeType = "";
			String sFileExt = "";
			String sMimeExt = "";

			// Convert to lowercase
			sRequestedFile = sRequestedFile.ToLower();

			int iStartPos = sRequestedFile.IndexOf(".");

			sFileExt = sRequestedFile.Substring(iStartPos);

			try
			{
				//Open the Vdirs.dat to find out the list virtual directories
				sr = new StreamReader("data\\Mime.Dat");

				while ((sLine = sr.ReadLine()) != null)
				{

					sLine.Trim();

					if (sLine.Length > 0)
					{
						//find the separator
						iStartPos = sLine.IndexOf(";");

						// Convert to lower case
						sLine = sLine.ToLower();

						sMimeExt = sLine.Substring(0, iStartPos);
						sMimeType = sLine.Substring(iStartPos + 1);

						if (sMimeExt == sFileExt)
							break;
					}
				}
			}
			catch (Exception e)
			{
				Console.WriteLine("An Exception Occurred : " + e.ToString());
			}

			if (sMimeExt == sFileExt)
				return sMimeType;
			else
				return "";
		}



		/// <summary>
		/// Returns the Physical Path
		/// </summary>
		/// <param name="sMyWebServerRoot">Web Server Root Directory</param>
		/// <param name="sDirName">Virtual Directory </param>
		/// <returns>Physical local Path</returns>
		public string GetLocalPath(string sMyWebServerRoot, string sDirName)
		{

			StreamReader sr;
			String sLine = "";
			String sVirtualDir = "";
			String sRealDir = "";
			int iStartPos = 0;


			//Remove extra spaces
			sDirName.Trim();



			// Convert to lowercase
			sMyWebServerRoot = sMyWebServerRoot.ToLower();

			// Convert to lowercase
			sDirName = sDirName.ToLower();

			//Remove the slash
			//sDirName = sDirName.Substring(1, sDirName.Length - 2);


			try
			{
				//Open the Vdirs.dat to find out the list virtual directories
				sr = new StreamReader("data\\VDirs.Dat");

				while ((sLine = sr.ReadLine()) != null)
				{
					//Remove extra Spaces
					sLine.Trim();

					if (sLine.Length > 0)
					{
						//find the separator
						iStartPos = sLine.IndexOf(";");

						// Convert to lowercase
						sLine = sLine.ToLower();

						sVirtualDir = sLine.Substring(0, iStartPos);
						sRealDir = sLine.Substring(iStartPos + 1);

						if (sVirtualDir == sDirName)
						{
							break;
						}
					}
				}
			}
			catch (Exception e)
			{
				Console.WriteLine("An Exception Occurred : " + e.ToString());
			}


			Console.WriteLine("Virtual Dir : " + sVirtualDir);
			Console.WriteLine("Directory   : " + sDirName);
			Console.WriteLine("Physical Dir: " + sRealDir);
			if (sVirtualDir == sDirName)
				return sRealDir;
			else
				return "";
		}



		/// <summary>
		/// This function send the Header Information to the client (Browser)
		/// </summary>
		/// <param name="sHttpVersion">HTTP Version</param>
		/// <param name="sMIMEHeader">Mime Type</param>
		/// <param name="iTotBytes">Total Bytes to be sent in the body</param>
		/// <param name="mySocket">Socket reference</param>
		/// <returns></returns>
		public void SendHeader(string sHttpVersion, string sMIMEHeader, int iTotBytes, string sStatusCode, ref Socket mySocket)
		{

			String sBuffer = "";

			// if Mime type is not provided set default to text/html
			if (sMIMEHeader.Length == 0)
			{
				sMIMEHeader = "text/html";  // Default Mime Type is text/html
			}

			sBuffer = sBuffer + sHttpVersion + sStatusCode + "\r\n";
			sBuffer = sBuffer + "Server: EXE-Connector\r\n";
			sBuffer = sBuffer + "Content-Type: " + sMIMEHeader + "\r\n";
			sBuffer = sBuffer + "Accept-Ranges: bytes\r\n";
			sBuffer = sBuffer + "Content-Length: " + iTotBytes + "\r\n\r\n";

			Byte[] bSendData = Encoding.ASCII.GetBytes(sBuffer);

			SendToBrowser(bSendData, ref mySocket);

			Console.WriteLine("Total Bytes : " + iTotBytes.ToString());

		}



		/// <summary>
		/// Overloaded Function, takes string, convert to bytes and calls 
		/// overloaded sendToBrowserFunction.
		/// </summary>
		/// <param name="sData">The data to be sent to the browser(client)</param>
		/// <param name="mySocket">Socket reference</param>
		public void SendToBrowser(String sData, ref Socket mySocket)
		{
			SendToBrowser(Encoding.ASCII.GetBytes(sData), ref mySocket);
		}



		/// <summary>
		/// Sends data to the browser (client)
		/// </summary>
		/// <param name="bSendData">Byte Array</param>
		/// <param name="mySocket">Socket reference</param>
		public void SendToBrowser(Byte[] bSendData, ref Socket mySocket)
		{
			int numBytes = 0;

			try
			{
				if (mySocket.Connected)
				{
					if ((numBytes = mySocket.Send(bSendData, bSendData.Length, 0)) == -1)
						Console.WriteLine("Socket Error cannot Send Packet");
					else
					{
						Console.WriteLine("No. of bytes send {0}", numBytes);
					}
				}
				else
					Console.WriteLine("Connection Dropped....");
			}
			catch (Exception e)
			{
				Console.WriteLine("Error Occurred : {0} ", e);

			}
		}


		//This method Accepts new connection and
		//First it receives the welcome massage from the client,
		//Then it sends the Current date time to the Client.
		public void StartListen()
		{

			int iStartPos = 0;
			String sRequest;
			String sDirName;
			String sRequestedFile;
			String sErrorMessage;
			String sLocalDir;
			String sMyWebServerRoot = "wRoot\\";
			String sPhysicalFilePath = "";
			String sFormattedMessage = "";
			String sResponse = "";



			while (true)
			{
				//Accept a new connection
				Socket mySocket = myListener.AcceptSocket();

				Console.WriteLine("Socket Type " + mySocket.SocketType);
				if (mySocket.Connected)
				{
					Console.WriteLine("\nClient Connected!!\n==================\nCLient IP {0}\n",
						mySocket.RemoteEndPoint);



					//make a byte array and receive data from the client 
					Byte[] bReceive = new Byte[1024];
					int i = mySocket.Receive(bReceive, bReceive.Length, 0);
					int a2 = 0;


					//Convert Byte to String
					string sBuffer = Encoding.ASCII.GetString(bReceive);



					//At present we will only deal with GET type
					if (sBuffer.Substring(0, 3) != "GET")
					{
						Console.WriteLine("Only Get Method is supported..");
						//mySocket.Close();
						//return;
						continue;
					}


					// Look for HTTP request
					iStartPos = sBuffer.IndexOf("HTTP", 1);


					// Get the HTTP text and version e.g. it will return "HTTP/1.1"
					string sHttpVersion = sBuffer.Substring(iStartPos, 8);


					// Extract the Requested Type and Requested file/directory
					sRequest = sBuffer.Substring(0, iStartPos - 1);

					//To send to PUTTY telnet session out data, should give them as parameter...
					// Let's separate request to file & data parts...
					a2 = sRequest.IndexOf("OUT_BUFFER=");
					if (a2 >= 0)
					{
						OUT_BUFFER = sRequest.Substring(a2 + 11);
						OUT_BUFFER = Uri.UnescapeDataString(OUT_BUFFER);

						sRequest = sRequest.Substring(0, a2 - 1);   // one for "?" in link
					}


					//Replace backslash with Forward Slash, if Any
					sRequest.Replace("\\", "/");

					//If file name is not supplied add forward slash to indicate 
					//that it is a directory and then we will look for the 
					//default file name..
					if ((sRequest.IndexOf(".") < 1) && (!sRequest.EndsWith("/")))
					{
						sRequest = sRequest + "/";
					}


					//Extract the requested file name
					iStartPos = sRequest.LastIndexOf("/") + 1;
					sRequestedFile = sRequest.Substring(iStartPos);


					//Extract The directory Name
					sDirName = sRequest.Substring(sRequest.IndexOf("/"), sRequest.LastIndexOf("/") - 3);



					/////////////////////////////////////////////////////////////////////
					// Identify the Physical Directory
					/////////////////////////////////////////////////////////////////////
					if (sDirName == "/")
						sLocalDir = sMyWebServerRoot;
					else
					{
						//Get the Virtual Directory
						sLocalDir = GetLocalPath(sMyWebServerRoot, sDirName);
					}


					Console.WriteLine("Directory Requested : " + sLocalDir);

					//If the physical directory does not exists then
					// dispaly the error message
					if (sLocalDir.Length == 0)
					{
						sErrorMessage = "<H2>Error!! Requested Directory does not exists</H2><Br>";
						//sErrorMessage = sErrorMessage + "Please check data\\Vdirs.Dat";

						//Format The Message
						SendHeader(sHttpVersion, "", sErrorMessage.Length, " 404 Not Found", ref mySocket);

						//Send to the browser
						SendToBrowser(sErrorMessage, ref mySocket);

						mySocket.Close();

						continue;
					}


					/////////////////////////////////////////////////////////////////////
					// Identify the File Name
					/////////////////////////////////////////////////////////////////////

					//If The file name is not supplied then look in the default file list
					if (sRequestedFile.Length == 0)
					{

						// Get the default filename
						sRequestedFile = GetTheDefaultFileName(sLocalDir);

						if (sRequestedFile == "")
						{
							sErrorMessage = "<H2>Error!! No Default File Name Specified</H2>";
							SendHeader(sHttpVersion, "", sErrorMessage.Length, " 404 Not Found", ref mySocket);
							SendToBrowser(sErrorMessage, ref mySocket);

							mySocket.Close();

							return;

						}
					}




					/////////////////////////////////////////////////////////////////////
					// Get TheMime Type
					/////////////////////////////////////////////////////////////////////

					String sMimeType = GetMimeType(sRequestedFile);



					//Build the physical path
					sPhysicalFilePath = sLocalDir + sRequestedFile;
					Console.WriteLine("File Requested : " + sPhysicalFilePath);

					String PUTTY_sign = "";

					// If incomming buffer from PUTTY telnet is requested to transfer...
					if (sRequestedFile == "IN_BUFFER.txt")
					{
						PUTTY_sign = "[IN-BUFFER-OK]" + "\n";
						var str_enc = PUTTY_sign + IN_BUFFER;
						SendHeader(sHttpVersion, sMimeType, str_enc.Length, " 200 OK", ref mySocket);
						SendToBrowser(str_enc, ref mySocket);
						IN_BUFFER = "";
					}
					else if (sRequestedFile == "OUT_BUFFER.txt")
					{

						PUTTY_sign = "[OUT-BUFFER-OK]" + "\n";
						SendHeader(sHttpVersion, sMimeType, PUTTY_sign.Length, " 200 OK", ref mySocket);
						SendToBrowser(PUTTY_sign, ref mySocket);

						// Unity3D forced close method on exit ;)
						if (OUT_BUFFER.IndexOf("EXIT_EXE") >= 0)
						{
							Console.WriteLine("Process is closed.");
							mySocket.Close();
							process.Kill();
							Environment.Exit(0);
						}
						else DataSend2();
						OUT_BUFFER = "";
					}
					else
					{

						if (File.Exists(sPhysicalFilePath) == false)
						{

							sErrorMessage = "<H2>404 Error! File Does Not Exists...</H2>";
							SendHeader(sHttpVersion, "", sErrorMessage.Length, " 404 Not Found", ref mySocket);
							SendToBrowser(sErrorMessage, ref mySocket);

							Console.WriteLine(sFormattedMessage);
						}

						else
						{
							int iTotBytes = 0;

							sResponse = "";

							FileStream fs = new FileStream(sPhysicalFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
							// Create a reader that can read bytes from the FileStream.


							BinaryReader reader = new BinaryReader(fs);
							byte[] bytes = new byte[fs.Length];
							int read;
							while ((read = reader.Read(bytes, 0, bytes.Length)) != 0)
							{
								// Read from the file and write the data to the network
								sResponse = sResponse + Encoding.ASCII.GetString(bytes, 0, read);

								iTotBytes = iTotBytes + read;

							}
							reader.Close();
							fs.Close();

							SendHeader(sHttpVersion, sMimeType, iTotBytes, " 200 OK", ref mySocket);
							SendToBrowser(bytes, ref mySocket);
							//mySocket.Send(bytes, bytes.Length,0);

						}
					}
					mySocket.Close();
				}
			}
		}

		// Returns IP number to listen

		public String GetIpNumber()
		{
			StreamReader sr;
			String Ip = "127.0.0.1";
			String sLine = "";

			try
			{
				//Open the port.dat to find out the port number
				sr = new StreamReader("data\\ip.dat");

				while ((sLine = sr.ReadLine()) != null)
				{
					Ip = sLine;
					Console.WriteLine("Current IP: " + Ip);
					break;
				}
			}
			catch (Exception e)
			{
				Console.WriteLine("Can't read IP number, an Exception Occurred : " + e.ToString());
			}
			return Ip;
		}


		// Returns Port number to listen

		public int GetPortNumber()
		{
			int portnum = 80;
			StreamReader sr;
			String sLine = "";

			try
			{
				//Open the port.dat to find out the port number
				sr = new StreamReader("data\\port.dat");

				while ((sLine = sr.ReadLine()) != null)
				{
					portnum = System.Convert.ToInt32(sLine);
					Console.WriteLine("Listening to port: " + portnum.ToString());
					break;
				}
			}
			catch (Exception e)
			{
				Console.WriteLine("Can't read port number, an Exception Occurred : " + e.ToString());
			}
			return portnum;
		}

		// Returns Arguments from file

		public String GetCmdExe()
		{
			StreamReader sr;
			String sLine = "";

			try
			{
				//Open the port.dat to find out the port number
				sr = new StreamReader("data\\Exe2Start.dat");

				while ((sLine = sr.ReadLine()) != null)
				{
					Console.WriteLine("Got EXE filename: " + sLine);
					break;
				}
			}
			catch (Exception e)
			{
				Console.WriteLine("Can't read EXE filename an Exception Occurred : " + e.ToString());
			}
			return sLine;
		}

		public String GetCmdArguments()
		{
			StreamReader sr;
			String sLine = "";

			try
			{
				//Open the port.dat to find out the port number
				sr = new StreamReader("data\\Arguments.dat");

				while ((sLine = sr.ReadLine()) != null)
				{
					Console.WriteLine("Got command line arguments: " + sLine);
					break;
				}
			}
			catch (Exception e)
			{
				Console.WriteLine("Can't read arguments an Exception Occurred : " + e.ToString());
			}
			return sLine;
		}

		public void StartExe()
		{
			try
			{

				process = new Process();

				process.StartInfo.CreateNoWindow = true;

				process.StartInfo.WorkingDirectory = CurPath;
				process.StartInfo.FileName = CurPath + CmdExe;        // Telnet app...
				process.StartInfo.Arguments = CmdArgs;

				//process.StartInfo.UseShellExecute = true;
				process.StartInfo.UseShellExecute = false;

				process.StartInfo.RedirectStandardInput = true;

				//process.EnableRaisingEvents = false;
				//process.Exited += new EventHandler(ProcessExited);

				process.StartInfo.RedirectStandardError = true;

				// Async. not working, use sync. input-output only...


				process.StartInfo.RedirectStandardOutput = true;

				process.OutputDataReceived += new DataReceivedEventHandler(DataReceived);
				process.ErrorDataReceived += new DataReceivedEventHandler(ErrorReceived);

				process.Start();

				process.BeginOutputReadLine();

				WriteStream = process.StandardInput;        // write to stdin of process...
				WriteStream.AutoFlush = true;

				//WriteStream.WriteLine("input input.txt\n");


				//ReadStream = process.StandardOutput;        // read from stdout of process...

				//      System.IO.File.WriteAllText("output2.txt", ReadStream.ReadToEnd());
				Console.WriteLine("EXE started -ok...");
				process.WaitForExit();
				Console.WriteLine("EXE finished -ok...");
			}
			catch (Exception e)
			{
				Console.WriteLine("Unable to launch EXE: " + e.Message);
				// Environment.Exit(0);
			}
		}

		void DataReceived(object sender, DataReceivedEventArgs eventArgs)
		{
			Console.Write("r"); IN_BUFFER += (eventArgs.Data + "\n");
		}

		void ErrorReceived(object sender, DataReceivedEventArgs eventArgs)
		{ Console.WriteLine("error:" + eventArgs.Data); }

		void DataSend2()
		{
			String ss = OUT_BUFFER + "\n";
			String ss2 = "";
			Boolean cwas = false;
			for (int i = 0; i < ss.Length - 1; i++)
			{
				if (ss.Substring(i, 2) == "\\" + "n")
				{
					WriteStream.WriteLine(ss2 + "\n");
					Console.WriteLine("cmd:{0}", ss2);
					System.Threading.Thread.Sleep(100);
					ss2 = ""; i++; cwas = true;
				}
				else ss2 += ss.Substring(i, 1);
			}

			if (!cwas) WriteStream.WriteLine(OUT_BUFFER + "\n");  // if cant separate
			Console.Write("s");

		}

		//void ProcessExited()
		//{ Environment.Exit(0); }

		// From outside...(not used)
		//public void OnApplicationQuit()
		//{
		// if( process != null && !process.HasExited ) process.Kill();
		//}


	}
}
