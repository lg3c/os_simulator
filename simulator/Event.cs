using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Simulator
{
    /// <summary>
    /// Identificador do evento.
    /// </summary>
    enum EventIdentifier
    {
        SimulationStart,    // evento auxiliar representando inicio da simulacao
        Event1,
        Event2,
        Event3,             // eventos 1 a 7 estao definidos de acordo
        Event4,             // com o especificado no artigo de apoio
        Event5Io,           // (exceto eventos 5 e 6, que foram cada um separados em dois)
        Event5Disk,
        Event6Io,
        Event6Disk,
        Event7,
        SimulationEnd,      // evento auxiliar representando final da simulacao
        Interr              // evento auxiliar representando interrupcao do processador
    }
    
    /// <summary>
    /// Representação do evento no sistema.
    /// </summary>
    class Event
    {
        internal EventIdentifier    eventId;
        internal int                eventTime, jobIndex;        
    }
}
