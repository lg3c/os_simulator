using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Simulator
{
    /// <summary>
    /// Representação da memória no sistema.
    /// </summary>
    class Memory : MemoryEntity
    {
        internal Memory(Simulator _simulator, int _totalSpace, int _reallocationDelay) :
            base(_simulator, _totalSpace, _reallocationDelay)
        {
        }
        
        /// <summary>
        /// Aloca o segmento a ser usado pelo job na memória.
        /// </summary>
        /// <param name="_jobIndex">O índice do job requisitando alocação.</param>
        internal void allocate(int _jobIndex)
        {
            Job job = Simulator.JobTable[_jobIndex];
            MemoryPartition partition = new MemoryPartition()
            {
                JobIndex = _jobIndex,
                Start = 0,
                End = job.SegmentTree[job.CurrentSegmentIndex].Size,
                JobPartitionIndex = job.CurrentSegmentIndex,
                Active = true,
                AllocationTime = Simulator.Clock
            };

            base.allocate(partition);
        }

        /// <summary>
        /// Desaloca um segmento da memória e agenda o evento de requisição de memória para o próximo elemento da fila, se houver.
        /// </summary>
        /// <param name="_jobIndex">Índice do job dono do segmento a ser desalocado.</param>
        /// <param name="_partitionIndex">Índice do segmento a ser desalocado.</param>
        /// <returns>Log da execução.</returns>
        internal override string deallocate(int _jobIndex, int _partitionIndex)
        {
            string result = string.Empty;

            MemoryPartition partition = PartitionTable.First(s => s.JobIndex == _jobIndex && s.JobPartitionIndex == _partitionIndex);
            PartitionTable.Remove(partition);

            QueueElement queueElement = Queue.dequeue();
            if (queueElement != null)
            {
                Simulator.scheduleEvent(new Event()
                {
                    eventId = EventIdentifier.Event2,
                    eventTime = Simulator.Clock,
                    jobIndex = queueElement.jobIndex
                });
                result = string.Format(" e job {0} tirado da fila e agendado para evento 2", queueElement.jobIndex);
            }

            return result;
        }

        /// <summary>
        /// Retorna uma visualização da memória.
        /// </summary>
        /// <returns>Visualização da memória.</returns>
        internal string memoryView()
        {
            StringBuilder memoryView = new StringBuilder();
            memoryView.AppendLine("visualizacao da memoria");
            memoryView.AppendLine(string.Format("capacidade = {0} | ocupado = {1} | livre = {2}",
                TotalSpace, TotalSpace - availableSpace, availableSpace));
            memoryView.AppendLine("particoes ocupadas:");

            foreach (MemoryPartition segment in PartitionTable)
            {
                memoryView.AppendLine(string.Format("[job {0} segmento {1} tamanho {2} {3}]",
                    segment.JobIndex, segment.JobPartitionIndex, segment.Size, segment.Active ? "ativo" : "inativo"));
            }

            return memoryView.ToString();
        }

        /// <summary>
        /// Verifica se o segmento atual do job está carregado na memória.
        /// </summary>
        /// <param name="_jobIndex">O job a ter o segmento verificado.</param>
        /// <returns>True se segmento estiver carregado, caso contrário, false.</returns>
        internal bool isPartitionLoaded(int _jobIndex)
        {
            MemoryPartition partition = Simulator.CentralMemory.PartitionTable.FirstOrDefault(s => s.JobIndex == _jobIndex
                && s.JobPartitionIndex == Simulator.JobTable[_jobIndex].CurrentSegmentIndex);
            if (partition != null)
            {
                partition.Active = true;
                return true;
            }
            return false;
        }
    }
}
