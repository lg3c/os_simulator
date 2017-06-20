using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Simulator
{
    /// <summary>
    /// Representação do processador no sistema.
    /// </summary>
    class Processor
    {
        readonly Simulator      Simulator;
        internal int            JobIndex;   // -1 indica que processador esta livre                                
        readonly internal int   TimeSlice,
                                MaxConcurrentJobs;
        internal Queue          Queue;

        /// <summary>
        /// Indica se ainda há "vagas" para o processador sem exceder o limite de multiprogramação.
        /// </summary>
        internal bool IsSlotAvailable
        {
            get
            {
                return (Simulator.JobTable.Count(j => j.Interrupted == true) < MaxConcurrentJobs);
            }            
        }

        /// <summary>
        /// Construtor da classe.
        /// </summary>
        /// <param name="_simulator">Instância do simulador.</param>
        /// <param name="_timeSlice">Duração do time slice do processador.</param>
        /// <param name="_maxConcurrentJobs">Limite de multiprogramação do processador.</param>
        internal Processor(Simulator _simulator, int _timeSlice, int _maxConcurrentJobs)
        {
            Simulator = _simulator;
            TimeSlice = _timeSlice;
            MaxConcurrentJobs = _maxConcurrentJobs;            
            JobIndex = -1;
            Queue = new Queue(Simulator);
        }

        /// <summary>
        /// Requisita o processador para o job dado, e agenda evento de interrupção ao fim do time slice.
        /// </summary>
        /// <param name="_jobIndex">Índice na tabela de jobs do job fazendo a requisição.</param>
        internal void request(int _jobIndex)
        {
            JobIndex = _jobIndex;            

            Simulator.scheduleEvent(new Event()
            {
                eventId = EventIdentifier.Interr,
                eventTime = Simulator.Clock + TimeSlice,      
                jobIndex = _jobIndex
            });
        }

        /// <summary>
        /// Libera o processador, cancela um possível evento de interrupção agendado para o job, e agenda o evento de requisição para o próximo job da fila, se houver.
        /// </summary>
        /// <param name="_scheduleTime">Intervalo para agendamento do evento de requisição.</param>
        /// <returns></returns>
        internal string release(int _scheduleTime)
        {
            string result = string.Empty;

            Event interrupt = Simulator.EventList.FirstOrDefault
            (
                e => e.jobIndex == this.JobIndex 
                && e.eventId == EventIdentifier.Interr 
                && e.eventTime > Simulator.Clock
            );
            if (interrupt != null)
            {
                Simulator.EventList.Remove(interrupt);
            }

            JobIndex = -1;                      

            QueueElement queueElement = Queue.dequeue();
            if (queueElement != null)
            {
                Simulator.scheduleEvent(new Event()
                {
                    eventId = EventIdentifier.Event3,
                    eventTime = _scheduleTime,
                    jobIndex = queueElement.jobIndex
                });
                result = string.Format(" e job {0} tirado da fila e agendado para evento 3", queueElement.jobIndex);
            }

            return result;
        }
    }
}
