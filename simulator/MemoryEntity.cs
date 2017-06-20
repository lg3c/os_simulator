using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Simulator
{
    /// <summary>
    /// Representação de uma entidade de memória genérica no sistema, usada para modelagem da memória principal e do disco.
    /// </summary>
    class MemoryEntity
    {
        internal readonly Simulator Simulator;
        
        internal readonly int TotalSpace, ReallocationDelay;
        internal LinkedList<MemoryPartition> PartitionTable;
        internal Queue Queue;

        /// <summary>
        /// Indica o espaço disponível na entidade de memória.
        /// </summary>
        internal int availableSpace
        {
            get
            {
                return TotalSpace - PartitionTable.Sum(s => s.Size);
            }            
        }                

        /// <summary>
        /// Construtor da classe.
        /// </summary>
        /// <param name="_simulator">Instância do simulador.</param>
        /// <param name="_totalSpace">Espaço total da entidade de memória.</param>
        /// <param name="_reallocationDelay">Atraso para realocação de partições da entidade de memória.</param>
        internal MemoryEntity(Simulator _simulator, int _totalSpace, int _reallocationDelay)
        {
            Simulator = _simulator;

            TotalSpace = _totalSpace;
            ReallocationDelay = _reallocationDelay;

            PartitionTable = new LinkedList<MemoryPartition>();
            Queue = new Queue(Simulator);            
        }        

        /// <summary>
        /// Aloca partição na entidade de memória, após desfragmentação e ajuste de posição (ínício e fim) da partição.
        /// </summary>
        /// <param name="_partition">Partição a ser alocada.</param>
        internal void allocate(MemoryPartition _partition)
        {
            defrag();

            int startPosition = PartitionTable.Any() ? PartitionTable.Last.Value.End + 1 : 0;
            _partition.Start = startPosition;
            _partition.End += startPosition;            
                
            PartitionTable.AddLast(_partition);                     
        }

        /// <summary>
        /// Desaloca todas as partições referentes a um job dado da entidade de memória.
        /// </summary>
        /// <param name="_jobIndex">Job a ter partições desalocadas.</param>
        /// <returns>Log da execução.</returns>
        internal string deallocateAll(int _jobIndex)
        {
            StringBuilder result = new StringBuilder();
            
            for (int i = PartitionTable.Count - 1; i >= 0; i--)
            {
                if (PartitionTable.ElementAt(i).JobIndex == _jobIndex)
                {
                    result.Append(deallocate(_jobIndex, PartitionTable.ElementAt(i).JobPartitionIndex));
                }                
            }            

            return result.ToString();
        }

        /// <summary>
        /// Desaloca a partição inativa da entidade de memória que foi alocada mais cedo.
        /// </summary>
        internal void deallocateFirstAllocated()
        {
            MemoryPartition firstInPartition = PartitionTable.Where(s => s.Active == false).OrderBy(s => s.AllocationTime).First();
            deallocate(firstInPartition.JobIndex, firstInPartition.JobPartitionIndex);
        }

        /// <summary>
        /// Desaloca uma partição da entidade de memória.
        /// </summary>
        /// <param name="_jobIndex">Índice do job dono da partição a ser desalocada.</param>
        /// <param name="_partitionIndex">Índice da partição a ser desalocada.</param>
        /// <returns>Log da execução.</returns>
        internal virtual string deallocate(int _jobIndex, int _partitionIndex)
        {
            MemoryPartition partition = PartitionTable.First(s => s.JobIndex == _jobIndex && s.JobPartitionIndex == _partitionIndex);
            PartitionTable.Remove(partition);
            
            return string.Empty;
        }

        /// <summary>
        /// Desaloca todas as partições inativas da entidade de memória.
        /// </summary>
        internal void cleanUp()
        {
            while (PartitionTable.Any(s => s.Active == false) && Queue.Any())
            {
                deallocateFirstAllocated();
            }
        }

        /// <summary>
        /// Desfragmenta a entidade de memória.
        /// </summary>
        private void defrag()
        {
            int start = 0;

            LinkedListNode<MemoryPartition> partition = PartitionTable.First;                        

            while (partition != null)
            {
                if (partition.Value.Start > start)
                {
                    int segmentSize = partition.Value.Size;
                    partition.Value.Start = start;
                    partition.Value.End = start + segmentSize;                    
                }

                start = partition.Value.End + 1;
                partition = partition.Next;
            }
        }
    }

    /// <summary>
    /// Tipo de acesso permitido aos arquivos.
    /// </summary>
    enum AccessType
    {
        Public,
        Private
    }
    
    /// <summary>
    /// Representação no sistema de uma partição de entidade de memória.
    /// </summary>
    class MemoryPartition
    {
        internal int        Start, End, JobIndex, JobPartitionIndex, AllocationTime;
        internal string     FileName;
        internal AccessType AccessType;
        internal bool       Active;
     
        /// <summary>
        /// Indica o tamanho da partição.
        /// </summary>
        internal int Size
        {
            get
            {
                return End - Start;
            }
            private set { }
        }
    }
}
