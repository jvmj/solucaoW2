using System;
using System.Collections.Generic;
using System.Linq;

/*
 * Comunicação utilizada: síncrona (com leitura serial, linha a linha, do arquivo .txt)
 * 
 * */
namespace solucaow2
{
    class Program
    {

        static void Main(string[] args)
        {

            //Testa se o arquivo txt foi informado
            //Se nao foi informado, não vai funcionar
            if (args.Length == 0)
            {
                System.Console.WriteLine("Você precisa inserir um caminho de arquivo válido.");
            }

            //Senao, leia o arquivo informado
            else
            {
           string ip, porta, indiceInicial, indiceFinal;

                string[] linhas = System.IO.File.ReadAllLines(args[0]);

                List<string> resultadoLinha = new List<string>();
                List<List<string>> listaDeLinhas = new List<List<string>>();

                foreach (string linha in linhas)
                {
                    Char espaco = ' ';
                    String[] substrings = linha.Split(espaco);

                    ip = substrings[0];
                    porta = substrings[1];
                    indiceInicial = substrings[2];
                    indiceFinal = substrings[3];

                    Console.WriteLine(ip + " " + porta + " " + indiceInicial + " " + indiceFinal);

                    ClienteSincrono client = new ClienteSincrono();

                    //funcao que executa as requisicoes de cada linha do arquivo e retorma uma lista de strings
                    resultadoLinha = client.ExecutarConexao(ip, porta, indiceInicial, indiceFinal);


                    //adicionando o resultado em uma lista de lista de strings
                    listaDeLinhas.Add(resultadoLinha);

                }

                //juntando a lista de lista de strings em uma lista só para gerar um csv
                List<string> csvFinal = listaDeLinhas.SelectMany(x => x).ToList();

                //salvando o arquivo no diretorio onde o programa está rodando
                string fileName = String.Format(@"{0}\resultado.csv", AppDomain.CurrentDomain.BaseDirectory);

                System.IO.File.WriteAllLines(fileName, csvFinal);
                System.Console.WriteLine("Processamento concluido. Pressione qualquer tecla para sair.");

                Console.ReadKey();

            }
        }
    }
}
