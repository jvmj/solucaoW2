using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Linq;
using System.Collections.Generic;

namespace solucaow2
{
    public class ClienteSincrono
    {

        public List<string> ExecutarConexao(string ip, string porta, string indiceInicial, string indiceFinal)
        {
            // Data buffer for incoming data.  
            byte[] bytesNumSerie = new byte[1024];
            byte[] bytesStatus = new byte[1024];
            byte[] bytesRegistro = new byte[1024];
            byte[] bytesDataHora = new byte[1024];
            byte[] bytesValorRegistro = new byte[1024];
            List<string> dadosCsv = new List<string>();

            // Connect to a remote device.  
            try
            {
                int p = Int32.Parse(porta);
                IPAddress ipAddress = IPAddress.Parse(ip);
                IPEndPoint remoteEP = new IPEndPoint(ipAddress, p);

                // Create a TCP/IP  socket.  
                Socket sender = new Socket(ipAddress.AddressFamily,
                    SocketType.Stream, ProtocolType.Tcp);

                // Connect the socket to the remote endpoint. Catch any errors.  
                try
                {
                    sender.Connect(remoteEP);

                    Console.WriteLine("Socket connected to {0}",
                        sender.RemoteEndPoint.ToString());
                    
                    //1 - LENDO O NUMERO DA SERIE

                    // Encode the data string into a byte array.
                    byte[] msg_numeroSerie = { 0x7D, 00, 01, 01 };
                    
                    
                    // Send the data through the socket.  
                    int bytesSentNumSerie = sender.Send(msg_numeroSerie);

                    // Receive the response from the remote device.  
                    int bytesRecNumSerie = sender.Receive(bytesNumSerie);

                    byte[] byteNumSerieFinal = new byte[15];
                    Buffer.BlockCopy(bytesNumSerie, 3, byteNumSerieFinal, 0, 15);

                    Console.WriteLine("Numero da serie = {0}", Encoding.ASCII.GetString(byteNumSerieFinal, 0, 15));

                    //adicionando o numero da serie a lista a ser exportada
                    dadosCsv.Add(Encoding.ASCII.GetString(byteNumSerieFinal, 0, 15));


                    
                    //2 - LENDO O STATUS DOS REGISTROS

                    // Encode the data string into a byte array.
                    byte[] msg_statusRegistro = { 0x7D, 00, 02, 02 };


                    // Send the data through the socket.  
                    int bytesSentStatus = sender.Send(msg_statusRegistro);

                    // Receive the response from the remote device.  
                    int bytesRecStatus = sender.Receive(bytesStatus);
                    byte[] byteStatusIndiceAntigo = new byte[2];
                    Buffer.BlockCopy(bytesStatus, 3, byteStatusIndiceAntigo, 0, 2);
                    byte[] byteStatusIndiceRecente = new byte[2];
                    Buffer.BlockCopy(bytesStatus, 5, byteStatusIndiceRecente, 0, 2);
                    
                    //tratando os indices para o formato adequado
                    string x = BitConverter.ToString(byteStatusIndiceAntigo);
                    string y = x.Replace("-", "");
                    string z = BitConverter.ToString(byteStatusIndiceRecente);
                    string w = z.Replace("-", "");

                    int valorIndiceAntigo = Int32.Parse(y, System.Globalization.NumberStyles.HexNumber);
                    int valorIndiceRecente = Int32.Parse(w, System.Globalization.NumberStyles.HexNumber);

                    Console.WriteLine("Registro mais antigo = {0} -- Registro mais recente = {1}", valorIndiceAntigo, valorIndiceRecente);


                    //3 - DEFININDO O REGISTRO A SER LIDO

                    //converte os indices informados para inteiro
                    int indiceinicialInt = Int32.Parse(indiceInicial);
                    int indiceFinalInt = Int32.Parse(indiceFinal);
                    //variável auxiliar que representa o registro que está sendo lido no momento
                    int registroAtual = indiceinicialInt;

                    //varre dentro do intervalo informado
                    for (int i = indiceinicialInt; i <= indiceFinalInt; i++)
                    {
                        //primeiro testa se o registro atual está dentro do range
                        if (registroAtual >= valorIndiceAntigo && registroAtual <= valorIndiceRecente)
                        {

                            //obtendo o status do registro atual
                            string hexval = registroAtual.ToString("X");
                            char[] caracteres = hexval.ToCharArray();
                            string a, b;
                            //testando se o numero eh pequeno
                            if (caracteres.Length == 4)
                            {
                                a = String.Concat(caracteres[0], caracteres[1]);
                                b = String.Concat(caracteres[2], caracteres[3]);
                            }
                            else if (caracteres.Length == 2)
                            {
                                a = "00";
                                b = String.Concat(caracteres[0], caracteres[1]);
                            }
                            else
                            {
                                a = "00";
                                b = String.Concat("0", caracteres[0]);
                            }
                            byte byteStringA = Convert.ToByte(a, 16);
                            byte byteStringB = Convert.ToByte(b, 16);
                            int tamanhoMsgInt = caracteres.Length;
                            byte tamanhoMsgByte = Convert.ToByte(tamanhoMsgInt);
                            int checksumInteiro = 02 ^ 03 ^ byteStringA ^ byteStringB;
                            byte checksumByte = Convert.ToByte(checksumInteiro);
                            byte[] msg_Registro = { 0x7D, 02, 03, byteStringA, byteStringB, checksumByte };

                            // Send the data through the socket.  
                            int bytesSentRegistro = sender.Send(msg_Registro);

                            // Receive the response from the remote device.  
                            int bytesRecRegistro = sender.Receive(bytesRegistro);
                            byte[] bytesRetorno = new byte[1];
                            //capturando apenas o item do frame de resposta que interessa
                            Buffer.BlockCopy(bytesRegistro, 3, bytesRetorno, 0, 1);

                            Console.WriteLine("Status = {0}, Registro = {1}", bytesRetorno[0], registroAtual);

                            //Se o status for igual a zero, entao obtenha a data, hora e valor de cada registro
                            if (bytesRetorno[0] == 0)
                            {
                                //4 - LENDO DATA E HORA DO REGISTRO ATUAL

                                byte[] msg_dataHora = { 0x7D, 00, 04, 04 };


                                // Send the data through the socket.  
                                int bytesSentDataHora = sender.Send(msg_dataHora);

                                // Receive the response from the remote device.  
                                int bytesRecDataHora = sender.Receive(bytesDataHora);

                                byte[] byteDataHoraTrecho = new byte[5];
                                //extraindo o trecho com data e hora (5 bytes - 40 bits)
                                Buffer.BlockCopy(bytesDataHora, 3, byteDataHoraTrecho, 0, 5);

                                //convertendo a cadeia para bits no formato de string
                                string bitsString = string.Join("",
                                    byteDataHoraTrecho.Select(resultado => Convert.ToString(resultado, 2).PadLeft(8, '0')));

                                //extraindo cada fragmento para gerar a data
                                string anoString = bitsString.Substring(0, 12);
                                int ano = Convert.ToInt32(anoString, 2);

                                string mesString = string.Format("{0:00}", bitsString.Substring(12, 4));
                                int mesInt = Convert.ToInt32(mesString, 2);
                                string mes = mesInt.ToString("00");

                                string diaString = bitsString.Substring(16, 5);
                                int diaInt = Convert.ToInt32(diaString, 2);
                                string dia = diaInt.ToString("00");

                                string horaString = bitsString.Substring(21, 5);
                                int horaInt = Convert.ToInt32(horaString, 2);
                                string hora = horaInt.ToString("00");

                                string minutoString = bitsString.Substring(26, 6);
                                int minutoInt = Convert.ToInt32(minutoString, 2);
                                string minuto = minutoInt.ToString("00");

                                string segundoString = bitsString.Substring(32, 6);
                                int segundoInt = Convert.ToInt32(segundoString, 2);
                                string segundo = segundoInt.ToString("00");

                                //imprimindo a data
                                Console.WriteLine("Data: " + ano + "-" + mes + "-" + dia + " " + hora + ":" + minuto + ":" + segundo);


                                //5 - LENDO O VALOR DO REGISTRO

                                byte[] msg_valorRegistro = { 0x7D, 00, 05, 05 };

                                // Send the data through the socket.  
                                int bytesSentValorRegistro = sender.Send(msg_valorRegistro);

                                // Receive the response from the remote device.  
                                int bytesRecValorRegistro = sender.Receive(bytesValorRegistro);

                                //capturando os bytes com o valor do registro
                                byte[] byteValorRegistroFinal = new byte[4];
                                Buffer.BlockCopy(bytesValorRegistro, 3, byteValorRegistroFinal, 0, 3);
                                //byte[] byteValorRegistroFinal = { 0x43, 0xB6, 0xA8, 0xF6 };

                                if (BitConverter.IsLittleEndian)
                                {
                                    Array.Reverse(byteValorRegistroFinal);
                                }

                                float valorEnergiaFloat = BitConverter.ToSingle(byteValorRegistroFinal, 0);
                                double valorEnergiaArredondado = Math.Round(valorEnergiaFloat, 2, MidpointRounding.ToEven);
                                string valorEnergiaFormatado = String.Format("{0:0.00}", valorEnergiaArredondado);

                                //imprimindo o valor da energia
                                Console.WriteLine("Valor da energia: " + valorEnergiaFormatado);

                                dadosCsv.Add(registroAtual + ";" + ano + "-" + mes + "-" + dia + " " + hora + ":" + minuto + ":" + segundo + ";" + valorEnergiaFormatado);

                            }


                        }
                        registroAtual++;
                    }



                    // Release the socket.  
                    sender.Shutdown(SocketShutdown.Both);
                    sender.Close();


                }
                catch (ArgumentNullException ane)
                {
                    Console.WriteLine("ArgumentNullException : {0}", ane.ToString());
                }
                catch (SocketException se)
                {
                    Console.WriteLine("SocketException : {0}", se.ToString());
                }
                catch (Exception e)
                {
                    Console.WriteLine("Unexpected exception : {0}", e.ToString());
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            return dadosCsv;
            

        }


    }
}
