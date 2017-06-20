using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Simulator
{
    /// <summary>
    /// Representação de um elemento de uma fila no sistema.
    /// </summary>
    class QueueElement
    {
        internal int    jobIndex, entryTime;       
    }

    /// <summary>
    /// Representação de uma fila no sistema.
    /// </summary>
    class Queue : LinkedList<QueueElement>
    {
        readonly Simulator  Simulator;
        
        // dados de "cabeçalho" armazenados para cálculo de estatísticas, conforme indicação do artigo de apoio
        internal int        MaxWaitTime, 
                            WaitTimeAcc, 
                            MaxQueueLen, 
                            CurrentQueueLen, 
                            LenTimeProductAcc, 
                            LastEntryRemovalTime, 
                            EntryCount;

        /// <summary>
        /// Indica tamanho médio da fila.
        /// </summary>
        internal decimal MeanQueueLen
        {
            get
            {
                return EntryCount == 0 ? 0 : (decimal)LenTimeProductAcc / (decimal)Simulator.ElapsedTime;
            }            
        }

        /// <summary>
        /// Indica tempo médio de espera na fila.
        /// </summary>
        internal decimal MeanWaitingTime
        {
            get
            {
                return EntryCount == 0 ? 0 : (decimal)WaitTimeAcc / (decimal)EntryCount;
            }
        }
        
        /// <summary>
        /// Construtor da classe.
        /// </summary>
        /// <param name="_simulator">Instância do simulador.</param>
        internal Queue(Simulator _simulator)
        {
            Simulator = _simulator;
            MaxWaitTime = 0; 
            WaitTimeAcc = 0;
            MaxQueueLen = 0;
            CurrentQueueLen = 0; 
            LenTimeProductAcc = 0;
            LastEntryRemovalTime = 0;
            EntryCount = 0;
        }

        /// <summary>
        /// Retorna uma visualização da fila.
        /// </summary>
        /// <returns>Visualização da fila.</returns>
        internal string queueView()
        {
            StringBuilder queueView = new StringBuilder();
            queueView.AppendLine("visualizacao da fila do(a) {0}");
            queueView.AppendLine(string.Format("tamanho da fila = {0}", CurrentQueueLen));
            foreach (QueueElement element in this)
            {
                queueView.AppendLine(string.Format("[prioridade {0} job {1} instante de entrada {2}]",
                    Simulator.JobTable[element.jobIndex].Priority, element.jobIndex, element.entryTime));
            }
            return queueView.ToString();
        }

        /// <summary>
        /// Adiciona elemento à fila, no final ou logo à frente do primeiro elemento de prioridade mais baixa, se houver.
        /// Contabiliza valores para cálculo de estatísticas.
        /// </summary>
        /// <param name="_jobIndex">Job a ser adicionado à fila.</param>
        internal void enqueue(int _jobIndex)
        {
            QueueElement toAdd = new QueueElement()
            {
                jobIndex = _jobIndex,
                entryTime = Simulator.Clock
            };

            QueueElement next = this.FirstOrDefault(q => Simulator.JobTable[q.jobIndex].Priority > Simulator.JobTable[_jobIndex].Priority);
            
            if (next != null)
            {
                this.AddBefore(this.Find(next), toAdd);
            }
            else
            {
                this.AddLast(toAdd);
            }                        

            LenTimeProductAcc += (Simulator.Clock - LastEntryRemovalTime) * CurrentQueueLen;
            CurrentQueueLen++;
            MaxQueueLen = Math.Max(CurrentQueueLen, MaxQueueLen);
            EntryCount++;
            LastEntryRemovalTime = Simulator.Clock;
        }

        /// <summary>
        /// Tira o primeiro elemento da fila.
        /// Contabiliza valores para cálculo de estatísticas.
        /// </summary>
        /// <returns>Primeiro elemento da fila.</returns>
        internal QueueElement dequeue()
        {
            QueueElement queueElement = null;
            
            if (this.Any())
            {
                queueElement = this.First.Value;
                this.RemoveFirst();                

                LenTimeProductAcc += (Simulator.Clock - LastEntryRemovalTime) * CurrentQueueLen;
                CurrentQueueLen--;
                WaitTimeAcc += Simulator.Clock - queueElement.entryTime;
                MaxWaitTime = Math.Max(MaxWaitTime, Simulator.Clock - queueElement.entryTime);
                LastEntryRemovalTime = Simulator.Clock;
            }            

            return queueElement;
        }
    }
}
