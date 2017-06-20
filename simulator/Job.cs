using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Simulator
{
    /// <summary>
    /// Tipo de operação: leitura, acesso ao disco ou impressão.
    /// </summary>
    enum OperationType
    {
        Read,
        Disk,
        Print
    }
    
    /// <summary>
    /// Representação do job no sistema.
    /// </summary>
    internal class Job
    {        
        internal int    ArrivalTime, 
                        Priority, 
                        CpuTime, 
                        MemorySegmentCount,
                        CurrentSegmentIndex = 0,                        
                        ReadCount,
                        FileCount,
                        PrintCount,
                        RecordCount,                                                 
                        InterruptedRemainingTime;
        
        internal List<SegmentTreeNode> SegmentTree;
        internal List<File> FileList;

        internal bool Interrupted = false;

        internal IoDevice Device = null; // aponta para dispositivo de E/S em uso pelo job

        /// <summary>
        /// Indica a operação (E/S/disco) atual do job.
        /// É assumido que para cada job primeiro se executam todas as entradas, depois todos os acessos ao disco, depois todas as saídas.
        /// </summary>
        internal OperationType Operation
        {
            get 
            {
                if (RecordCount > FileCount + PrintCount)
                {
                    return OperationType.Read;
                }
                else if (RecordCount > PrintCount)
                {
                    return OperationType.Disk;
                }
                else
                {
                    return OperationType.Print;
                }
            }
        }         
       
        /// <summary>
        /// Indica o índice do arquivo atual do job na lista de arquivos.
        /// É assumido que cada acesso ao disco é feito a um arquivo distinto do job.
        /// </summary>
        internal int CurrentFileIndex
        {
            get
            {
                if (Operation == OperationType.Disk)
                {
                    return RecordCount - PrintCount - 1;
                }

                return 0;
            }
        }

        /// <summary>
        /// Determina intervalos aleatórios de pedidos aos dispositivos de E/S/disco.
        /// </summary>
        /// <returns>O intervalo para o pedido.</returns>
        internal int ioRequestInterval()
        {
            int requestInterval = Convert.ToInt32(-(CpuTime / RecordCount) * Math.Log10(new Random().NextDouble()));
            CpuTime = Math.Max(0, CpuTime - requestInterval);
            return requestInterval;
        }

        /// <summary>
        /// Determina aleatoriamente o próximo segmento a ser referenciado pelo job.
        /// Em 50% das vezes, job continua no segmento corrente.
        /// Em 25% das vezes, volta pro segmento pai, se existir.
        /// Em 25% das vezes, vai pra um dos segmentos filhos, se existir, com chances iguais de ir para qualquer um deles.
        /// </summary>
        internal void UpdateSegmentReferenced()
        {                                                
            Random random = new Random();            
            
            switch(random.Next(4))
            {
                case 0:
                    int fatherSegmentIndex = SegmentTree[CurrentSegmentIndex].FatherNodeIndex;
                    if (fatherSegmentIndex >= 0)
                    {
                        CurrentSegmentIndex = fatherSegmentIndex;
                    }
                    break;
                case 1:
                    var potentialSegments = SegmentTree.Where(s => s.FatherNodeIndex == CurrentSegmentIndex);
                    if (potentialSegments.Count() > 0)
                    {
                        CurrentSegmentIndex = SegmentTree.IndexOf(potentialSegments.ElementAt(random.Next(potentialSegments.Count())));
                    }
                    break;
            }
        }
    }

    /// <summary>
    /// Representação do nó da árvore de segmentos do job no sistema.
    /// </summary>
    internal class SegmentTreeNode
    {
        internal int Size, FatherNodeIndex;        
    }

    /// <summary>
    /// Representação de um arquivo do job no sistema.
    /// </summary>
    internal class File
    {
        internal AccessType AccessType;
        internal int Size;
        internal string Name;        
    }
}
