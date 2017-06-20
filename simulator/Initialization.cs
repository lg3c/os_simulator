using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Simulator
{
    partial class Simulator
    {
        /// <summary>
        /// Inicializa os principais parâmetros que caracterizam o simulador.
        /// A alteração de parâmetros do simulador para análise de seus efeitos deve ser feita aqui.
        /// </summary>
        void initialize()
        {
            while (!JobTable.Any())
            {
                readJobTableFromFile();
            }

            OverheadTime = 10; // esse é o overhead time citado no artigo de apoio

            scheduleIndependentEvents();

            CentralMemory = new Memory(this, 1024, 10);
            Cpu = new Processor(this, 5, 3);
            Disk = new Disk(this, 10240, 30);
            Readers = new IoDevice[] { new IoDevice(this, 50), new IoDevice(this, 60) };
            Printers = new IoDevice[] { new IoDevice(this, 70), new IoDevice(this, 80) };                
        }

        /// <summary>
        /// Lê a lista dos jobs a ser executada a partir de um arquivo de texto (.txt) localizado na pasta C:\simulador.
        /// 
        /// O início e o fim da simulação são representados por pseudo-jobs, com todos os parâmetros zerados, exceto o instante de chegada.
        /// O pseudo-job que tiver o instante de chegada menor sera o início, e o maior, o fim.
        /// 
        /// Para cada job, as especificações devem estar na forma abaixo (separadas entre si, quando na mesma linha, por TAB):
        /// 
        /// [instante de chegada][prioridade][tempo total de cpu][número de segmentos de memória][número de operações de entrada][numero de arquivos][número de operações de saída]      
        ///
        /// Obs.: o job será mais prioritário quanto menor for o valor da prioridade.
        ///
        /// Abaixo desse "cabeçalho" do job, devem constar linhas representando os segmentos do programa (uma pra cada), no formato:
        /// 
        /// [índice do segmento "pai"][tamanho]
        /// 
        /// O índice de cada segmento é sua posição na lista de segmentos do job no arquivo, começando em zero.
        /// Obs.: o segmento "pai" do segmento raiz deve ser -1.
        /// 
        /// Abaixo dos segmentos, devem constar as especificações dos arquivos usados pelo job, na forma:
        /// 
        /// [0 para público / 1 para privado][tamanho][nome]
        /// 
        /// Todos os parâmetros do arquivo de entrada de jobs devem ser números inteiros, exceto o nome do arquivo, que é uma string.        
        /// </summary>
        void readJobTableFromFile()
        {
            string line;

            Console.Write("nome do arquivo de entrada: "); // nome do arquivo de entrada deve ser digitado sem extensão
            string fileName = string.Format("c:\\simulador\\{0}.txt", Console.ReadLine());
            try
            {
                StreamReader file = new StreamReader(fileName);
                while ((line = file.ReadLine()) != null)
                {
                    string[] jobSpecs = line.Split(new char[] { '\t' });
                    Job job = new Job()
                    {
                        ArrivalTime = int.Parse(jobSpecs[0]),           // instante de chegada 
                        Priority = int.Parse(jobSpecs[1]),              // prioridade do job 
                        CpuTime = int.Parse(jobSpecs[2]),               // tempo de processamento
                        MemorySegmentCount = int.Parse(jobSpecs[3]),    // quantidade de segmentos de memoria
                        ReadCount = int.Parse(jobSpecs[4]),             // numero de operacoes de leitura
                        FileCount = int.Parse(jobSpecs[5]),             // numero de arquivos
                        PrintCount = int.Parse(jobSpecs[6])             // numero de operacoes de escrita
                    };
                    JobTable.Add(job);
                    
                    job.SegmentTree = new List<SegmentTreeNode>(job.MemorySegmentCount);
                    int segmentIndex = 0;
                    while (segmentIndex < job.MemorySegmentCount && ((line = file.ReadLine()) != null))
                    {
                        string[] segmentSpecs = line.Split(new char[] { '\t' });
                        job.SegmentTree.Add(new SegmentTreeNode()
                        {
                            FatherNodeIndex = int.Parse(segmentSpecs[0]),   // índice do segmento "pai"
                            Size = int.Parse(segmentSpecs[1])               // tamanho do segmento
                        });
                        segmentIndex++;
                    }

                    job.FileList = new List<File>(job.FileCount);
                    int fileIndex = 0;
                    while (fileIndex < job.FileCount && ((line = file.ReadLine()) != null))
                    {
                        string[] segmentSpecs = line.Split(new char[] { '\t' });
                        job.FileList.Add(new File()
                        {
                            AccessType = int.Parse(segmentSpecs[0]) == 0 ? AccessType.Public : AccessType.Private, // permissões de acesso ao arquivo
                            Size = int.Parse(segmentSpecs[1]),  // tamanho do arquivo
                            Name = segmentSpecs[2]  // nome do arquivo
                        });
                        fileIndex++;
                    }
                }
                file.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                JobTable.Clear();
            }            
        }

        /// <summary>
        /// Agenda os eventos independentes, que são o início e o fim da simulação e as chegadas dos jobs.
        /// </summary>
        void scheduleIndependentEvents()
        {
            // pseudo jobs que representam os instantes de inicio e fim da simulacao
            List<Job> specialJobs = new List<Job>();

            foreach (Job job in JobTable)
            {
                if (job.CpuTime != 0)
                {
                    scheduleEvent(new Event()
                    {
                        eventId = EventIdentifier.Event1,
                        eventTime = job.ArrivalTime,
                        jobIndex = JobTable.IndexOf(job)
                    });
                }
                else
                {
                    specialJobs.Add(job);
                }
            }

            // agenda início da simulação
            scheduleEvent(new Event()
            {
                eventId = EventIdentifier.SimulationStart,
                eventTime = specialJobs.OrderBy(j => j.ArrivalTime).First().ArrivalTime
            });

            // agenda fim da simulação
            scheduleEvent(new Event()
            {
                eventId = EventIdentifier.SimulationEnd,
                eventTime = specialJobs.OrderBy(j => j.ArrivalTime).Last().ArrivalTime
            });
        }
    }
}
