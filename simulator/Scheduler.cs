using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Simulator
{
    partial class Simulator
    {
        /// <summary>
        /// Percorre a lista de eventos do simulador, passando o controle às devidas rotinas de tratamento nos instantes especificados
        /// </summary>
        void scheduler()
        {
            // busca o pseudo evento de início da simulação
            Event currentEvent = EventList.First(e => e.eventId == EventIdentifier.SimulationStart);
            Clock = currentEvent.eventTime;
            Console.WriteLine(string.Format("\n{0}\t\t********** INSTANTE INICIAL DA SIMULACAO ***********\n",
                Clock));

            // seleciona o primeiro evento após início da simulação
            LinkedListNode<Event> currentEventNode = EventList.Find(currentEvent).Next;
            currentEvent = currentEventNode.Value;
            Clock = currentEvent.eventTime;

            // percorre a lista de eventos
            while (currentEvent.eventId != EventIdentifier.SimulationEnd)
            {
                string routine = string.Empty, result = string.Empty;

                switch (currentEvent.eventId)
                {
                    case EventIdentifier.Event1:
                        result = event1Routine(currentEvent);
                        routine = "event1Routine";
                        break;
                    case EventIdentifier.Event2:
                        result = event2Routine(currentEvent);
                        routine = "event2Routine";
                        break;
                    case EventIdentifier.Event3:
                        result = event3Routine(currentEvent);
                        routine = "event3Routine";
                        break;
                    case EventIdentifier.Event4:
                        result = event4Routine(currentEvent);
                        routine = "event4Routine";                        
                        break;
                    case EventIdentifier.Event5Io:
                        result = event5RoutineIo(currentEvent);
                        routine = "event5RoutineIo";
                        break;
                    case EventIdentifier.Event5Disk:
                        result = event5RoutineDisk(currentEvent);
                        routine = "event5RoutineDisk";
                        break;
                    case EventIdentifier.Event6Io:
                        result = event6RoutineIo(currentEvent);
                        routine = "event6RoutineIo";
                        break;
                    case EventIdentifier.Event6Disk:
                        result = event6RoutineDisk(currentEvent);
                        routine = "event6RoutineDisk";
                        break;
                    case EventIdentifier.Event7:
                        result = event7Routine(currentEvent);
                        routine = "event7Routine";
                        break;
                    case EventIdentifier.Interr:
                        result = interrRoutine(currentEvent);
                        routine = "interrRoutine";
                        break;
                }

                Console.WriteLine(string.Format("{0}\t\t{1}\t{2}\t{3}\t{4}",
                    Clock,
                    currentEvent.eventId,
                    currentEvent.jobIndex,
                    routine,
                    result));

                currentEventNode = currentEventNode.Next;
                currentEvent = currentEventNode.Value;
                Clock = currentEvent.eventTime;
            }

            Console.WriteLine(string.Format("\n{0}\t\t********** INSTANTE FINAL DA SIMULACAO ***********",
                Clock));
        }
    }
}
