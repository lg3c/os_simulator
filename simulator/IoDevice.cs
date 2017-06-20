using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Simulator
{
    /// <summary>
    /// Representação de um dispositivo de E/S (leitora/impressora) no sistema.
    /// </summary>
    class IoDevice
    {
        readonly Simulator      Simulator;
        readonly internal int   ProcessingTime;
        internal int            JobIndex;   // -1 indica que periferico esta livre
        internal Queue          Queue;

        /// <summary>
        /// Construtor da classe.
        /// </summary>
        /// <param name="_simulator">Instância do simulador.</param>
        /// <param name="_processingTime">Tempo de processamento do dispositivo.</param>
        internal IoDevice(Simulator _simulator, int _processingTime)
        {
            Simulator = _simulator;
            ProcessingTime = _processingTime;
            JobIndex = -1;
            Queue = new Queue(Simulator);
        }

        /// <summary>
        /// Requisita o dispositivo para o job dado.
        /// </summary>
        /// <param name="_jobIndex">Índice na tabela de jobs do job fazendo a requisição.</param>
        internal void request(int _jobIndex)
        {
            JobIndex = _jobIndex;
        }

        /// <summary>
        /// Libera o dispositivo e agenda o evento de requisição para o próximo job da fila, se houver.
        /// </summary>
        /// <param name="_scheduleTime">Intervalo para agendamento do evento de requisição.</param>
        /// <returns>Log da execução.</returns>
        internal string release(int _scheduleTime)
        {
            string result = string.Empty;

            JobIndex = -1;
            QueueElement queueElement = Queue.dequeue();
            if (queueElement != null)
            {
                Simulator.scheduleEvent(new Event()
                {
                    eventId = EventIdentifier.Event5Io,
                    eventTime = _scheduleTime,
                    jobIndex = queueElement.jobIndex
                });
                result = string.Format(" e job {0} tirado da fila e agendado para evento 5", queueElement.jobIndex);
            }

            return result;
        }
    }
}
