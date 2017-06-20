using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Simulator
{
    /// <summary>
    /// Representação do disco no sistema.
    /// </summary>
    class Disk : MemoryEntity
    {
        internal Disk(Simulator _simulator, int _totalSpace, int _reallocationDelay) :
            base(_simulator, _totalSpace, _reallocationDelay)
        {
        }

        /// <summary>
        /// Aloca o arquivo a ser usado pelo job no disco.
        /// </summary>
        /// <param name="_jobIndex">O índice do job requisitando alocação.</param>
        internal void allocate(int _jobIndex)
        {
            Job job = Simulator.JobTable[_jobIndex];
            MemoryPartition partition = new MemoryPartition()
            {
                JobIndex = _jobIndex,
                Start = 0,
                End = job.FileList[job.CurrentFileIndex].Size,
                JobPartitionIndex = job.CurrentFileIndex,
                Active = true,
                AllocationTime = Simulator.Clock,
                AccessType = job.FileList[job.CurrentFileIndex].AccessType,
                FileName = job.FileList[job.CurrentFileIndex].Name
            };

            base.allocate(partition);
        }

        /// <summary>
        /// Desaloca um arquivo do disco e agenda o evento de requisição de disco para o próximo elemento da fila, se houver.
        /// </summary>
        /// <param name="_jobIndex">Índice do job dono do arquivo a ser desalocado.</param>
        /// <param name="_partitionIndex">Índice do arquivo a ser desalocado.</param>
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
                    eventId = EventIdentifier.Event5Disk,
                    eventTime = Simulator.Clock,
                    jobIndex = queueElement.jobIndex
                });
                result = string.Format(" e job {0} tirado da fila e agendado para evento 5", queueElement.jobIndex);
            }

            return result;
        }

        /// <summary>
        /// Retorna uma visualização do disco.
        /// </summary>
        /// <returns>Visualização do disco.</returns>
        internal string diskView()
        {
            StringBuilder diskView = new StringBuilder();
            diskView.AppendLine("visualizacao do disco");
            diskView.AppendLine(string.Format("capacidade = {0} | ocupado = {1} | livre = {2}",
                TotalSpace, TotalSpace - availableSpace, availableSpace));
            diskView.AppendLine("particoes ocupadas:");

            foreach (MemoryPartition partition in PartitionTable)
            {
                diskView.AppendLine(string.Format("[job {0} arquivo {1} tamanho {2} {3} {4}]",
                    partition.JobIndex, partition.FileName, partition.Size, partition.AccessType, partition.Active ? "ativo" : "inativo"));
            }

            return diskView.ToString();
        }
    }
}
