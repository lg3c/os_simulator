using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Simulator
{                
    /// <summary>
    /// Classe principal do simulador, contendo o Main, o Scheduler, as rotinas de inicialização e as rotinas de tratamento de eventos.
    /// </summary>
    partial class Simulator
    {
        internal LinkedList<Event> EventList = new LinkedList<Event>();
        internal List<Job> JobTable = new List<Job>();        

        internal int Clock = 0, OverheadTime;

        internal Memory CentralMemory;
        internal Disk Disk;

        internal Processor Cpu;
                                            
        internal IoDevice[] Readers, 
                            Printers;

        /// <summary>
        /// Tempo decorrido desde o início da simulação.
        /// </summary>
        internal int ElapsedTime
        {
            get
            {
                return Clock - EventList.First(e => e.eventId == EventIdentifier.SimulationStart).eventTime;
            }
        }

        /// <summary>
        /// Ponto inicial do programa.
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            Simulator simulator = new Simulator();
            simulator.initialize();
            
            Console.WriteLine("\ninstante\tevento\tjob\trotina\t\tresultado");
            simulator.scheduler();

            simulator.summary();
            
            Console.ReadLine();
        }               
        
        /// <summary>
        /// Imprime as estatísticas finais de todas as filas ao fim da execução.
        /// </summary>
        void summary()
        {
            Console.WriteLine("\nestatisticas da fila da memoria");
            printSummary(CentralMemory.Queue);
            Console.WriteLine("\nestatisticas da fila do processador");
            printSummary(Cpu.Queue);
            Console.WriteLine("\nestatisticas da fila do disco");
            printSummary(Disk.Queue);
            Console.WriteLine("\nestatisticas da fila da leitora 1");
            printSummary(Readers[0].Queue);
            Console.WriteLine("\nestatisticas da fila da leitora 2");
            printSummary(Readers[1].Queue);
            Console.WriteLine("\nestatisticas da fila da impressora 1");
            printSummary(Printers[0].Queue);
            Console.WriteLine("\nestatisticas da fila da impressora 2");
            printSummary(Printers[1].Queue);
        }

        /// <summary>
        /// Imprime as estatísticas finais de cada fila.
        /// </summary>
        /// <param name="_queue">Fila para qual as estatísticas devem ser impressas.</param>
        void printSummary(Queue _queue)
        {
            Console.WriteLine(string.Format("\ttotal de entradas: {0}", _queue.EntryCount));
            Console.WriteLine(string.Format("\tcomprimento maximo: {0}", _queue.MaxQueueLen));
            Console.WriteLine(string.Format("\ttempo de espera maximo: {0}", _queue.MaxWaitTime));
            Console.WriteLine(string.Format("\ttempo de espera acumulado: {0}", _queue.WaitTimeAcc));
            Console.WriteLine(string.Format("\tcomprimento medio: {0}", _queue.MeanQueueLen));
            Console.WriteLine(string.Format("\ttempo de espera medio: {0}", _queue.MeanWaitingTime));
        }
    }
}