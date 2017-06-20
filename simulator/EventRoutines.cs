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
        /// Agenda o evento, ou seja, coloca-o na lista ligada de eventos na posição adequada, de acordo com seu instante de ocorrência.
        /// </summary>
        /// <param name="_event">Evento a ser agendado.</param>
        internal void scheduleEvent(Event _event)
        {
            Event next = EventList.FirstOrDefault(e => e.eventTime > _event.eventTime);
            if (next != null)
            {
                EventList.AddBefore(EventList.Find(next), _event);
            }
            else
            {
                EventList.AddLast(_event);
            }
        }        
        
        /// <summary>
        /// Corresponde ao evento 1 do artigo de apoio.
        /// São determinadas algumas características do job e o evento 2 é agendado.
        /// </summary>
        /// <param name="_event">Evento a ser tratado.</param>
        /// <returns>Log da execução.</returns>
        string event1Routine(Event _event)
        {            
            Job job = JobTable[_event.jobIndex];
            job.RecordCount = job.ReadCount + job.FileCount + job.PrintCount;            

            scheduleEvent(new Event()
            {
                eventId = EventIdentifier.Event2,
                eventTime = Clock,
                jobIndex = _event.jobIndex
            });

            return string.Format("evento 2 agendado para o job {0}", _event.jobIndex);
        }

        /// <summary>
        /// Corresponde ao evento 2 do artigo de apoio.
        /// Trata da requisição de memória para um segmento do job.
        /// Caso a alocação tenha sucesso, é agendado o evento 3 para o job.
        /// Caso contrário, o job é colocado na fila da memória.
        /// Marca outros segmentos do job que possam já estar alocados na memória como inativos.
        /// Desaloca segmentos inativos caso o segmento a ser alocado não caiba.
        /// No momento da alocação em si, é sempre feita uma desfragmentação.
        /// </summary>
        /// <param name="_event">Evento a ser tratado.</param>
        /// <returns>Log da execução.</returns>
        string event2Routine(Event _event)
        {
            string result;       

            Job job = JobTable[_event.jobIndex];

            // marca outros segmentos - que não o atual - pertencentes ao mesmo job que estejam na memória como inativos
            foreach (MemoryPartition segment in 
                CentralMemory.PartitionTable.Where(
                s => s.JobIndex == _event.jobIndex 
                    && s.JobPartitionIndex != job.CurrentSegmentIndex
                    && s.Active))
            {
                segment.Active = false;
            }

            // desaloca segmentos sem uso na memória caso o segmento a ser alocado não caiba
            while (CentralMemory.availableSpace < job.SegmentTree[job.CurrentSegmentIndex].Size
                && CentralMemory.PartitionTable.Any(s => s.Active == false))
            {
                CentralMemory.deallocateFirstAllocated();
            }
            
            // se o segmento couber na memória, faz a alocação e o job é agendado para o evento 3
            if (CentralMemory.availableSpace >= job.SegmentTree[job.CurrentSegmentIndex].Size)
            {                
                CentralMemory.allocate(_event.jobIndex);                

                scheduleEvent(new Event()
                {
                    eventId = EventIdentifier.Event3,
                    eventTime = Clock + CentralMemory.ReallocationDelay,
                    jobIndex = _event.jobIndex
                });

                result = string.Format("memoria alocada para segmento {0} e evento 3 agendado para o job {1}\n\n{2}", 
                    job.CurrentSegmentIndex, _event.jobIndex, CentralMemory.memoryView());
            }
            // se o segmento não couber na memória, o job é colocado na fila da memória
            else
            {
                CentralMemory.Queue.enqueue(_event.jobIndex);

                result = string.Format("job {0} colocado na fila de espera da memoria\n\n{1}", 
                    _event.jobIndex, string.Format(CentralMemory.Queue.queueView(), "memoria"));
            }

            return result;
        }

        /// <summary>
        /// Corresponde ao evento 3 do artigo de apoio.
        /// Trata da requisição do processador pelo job.
        /// Há controle de limite de multiprogramação, presença do segmento referenciado na memória, existência de operações de E/S/disco a realizar, de retomada de jobs interrompidos e de fila.
        /// Cobre a situação de interrupção do job em execução no processador no caso de chegada de um job de maior prioridade, conforme descrito no artigo de apoio.
        /// </summary>
        /// <param name="_event">Evento a ser tratado.</param>
        /// <returns>Log da execução.</returns>
        string event3Routine(Event _event)
        {
            string result;
            Job job = JobTable[_event.jobIndex];
            
            // se o processador está livre e o limite de multiprogramação não foi atingido
            if (Cpu.JobIndex < 0 && Cpu.IsSlotAvailable)
            {                                                               
                // se ainda há operações de E/S/disco a fazer
                if (job.RecordCount > 0)
                {
                    bool segmentLoaded;
                    int computeTime;                    

                    // tratamento especial para o caso de retomada de job interrompido, conforme descrito no artigo de apoio
                    if (job.Interrupted)
                    {
                        computeTime = job.InterruptedRemainingTime;
                        job.InterruptedRemainingTime = 0;
                        job.Interrupted = false;
                        segmentLoaded = true;
                        result = string.Format("processador realocado e evento 4 agendado para o job {0}", _event.jobIndex);
                    }
                    // se o segmento referenciado já está alocado na memória
                    else if (CentralMemory.isPartitionLoaded(_event.jobIndex))
                    {
                        computeTime = job.ioRequestInterval();
                        job.RecordCount--;                        
                        job.UpdateSegmentReferenced();                        
                        segmentLoaded = true;
                        result = string.Format("processador alocado e evento 4 agendado para o job {0}", _event.jobIndex);
                    }
                    // se o segmento referenciado não está alocado na memória
                    else
                    {
                        computeTime = 0;
                        segmentLoaded = false;                        
                        result = string.Format("segmento {0} ausente da memoria, evento 2 agendado para o job {1}", job.CurrentSegmentIndex, _event.jobIndex);
                    }

                    // se o segmento já está alocado na memória, aloca o processador
                    if (segmentLoaded)
                    {
                        Cpu.request(_event.jobIndex); 
                    }

                    // se o segmento está alocado, agenda o evento de liberação do processador (evento 4)
                    // caso contrário, agenda o de requisição de memória (evento 2)
                    scheduleEvent(new Event()
                    {
                        eventId = segmentLoaded ? EventIdentifier.Event4 : EventIdentifier.Event2,
                        eventTime = Clock + computeTime,
                        jobIndex = _event.jobIndex
                    });                    
                }
                // se não há mais operações de E/S/disco, agenda evento de encerramento do job
                else
                {
                    scheduleEvent(new Event()
                    {
                        eventId = EventIdentifier.Event7,
                        eventTime = Clock,
                        jobIndex = _event.jobIndex
                    });

                    result = string.Format("encerrando o job {0}, evento 7 agendado", _event.jobIndex);
                }
            }
            // se o processador não está livre e/ou o limite de multiprogramação foi atingido, coloca job na fila
            else
            {
                Cpu.Queue.enqueue(_event.jobIndex);

                // tratamento especial para caso de o processador estar ocupado por um job de menor prioridade que o que está requisitando
                // é agendada uma interrupção imediata do job corrente para alocação do de maior prioridade
                if (Cpu.JobIndex >= 0 && JobTable[Cpu.JobIndex].Priority > job.Priority)
                {
                    scheduleEvent(new Event()
                    {
                        eventId = EventIdentifier.Interr,
                        eventTime = Clock,
                        jobIndex = Cpu.JobIndex
                    });
                }

                result = string.Format("job {0} colocado na fila de espera do processador\n\n{1}", 
                    _event.jobIndex, string.Format(Cpu.Queue.queueView(), "processador"));
            }

            return result;
        }

        /// <summary>
        /// Corresponde ao evento 4 do artigo de apoio.
        /// Trata da liberação do processador pelo job, e agendamento do próximo evento (5), que é de E/S ou de disco.
        /// É assumido que para cada job são executadas primeiro todas as operações de entrada, depois todas de disco, depois todas de saída.
        /// </summary>
        /// <param name="_event">Evento a ser tratado.</param>
        /// <returns>Log da execução.</returns>
        string event4Routine(Event _event)
        {            
            int scheduleTime = Clock + OverheadTime;

            string partialResult = Cpu.release(scheduleTime);

            scheduleEvent(new Event()
            {
                eventId = JobTable[_event.jobIndex].Operation == OperationType.Disk ? EventIdentifier.Event5Disk : EventIdentifier.Event5Io,
                eventTime = scheduleTime,
                jobIndex = _event.jobIndex
            });

            return string.Format("processador liberado, evento 5 agendado para o job {0}{1}", _event.jobIndex, partialResult);
        }

        /// <summary>
        /// Corresponde a parte do evento 5 do artigo de apoio, que foi dividido em dois pela diferenciação entre dispositivos de E/S e o disco.
        /// Trata do início do processamento de um pedido de E/S. 
        /// O sistema simulado é composto de duas leitoras e duas impressoras. É assumido que as duas leitoras são equivalentes entre si, assim como as duas impressoras.
        /// Com isso, tenta-se alocar qualquer um dos dois dispositivos. 
        /// Se ambos estiverem livres, é alocado o primeiro deles. Se um estiver ocupado, é alocado o outro. Se ambos estiverem ocupados, é alocado o que tiver a menor fila.
        /// </summary>
        /// <param name="_event">Evento a ser tratado.</param>
        /// <returns>Log da execução.</returns>
        string event5RoutineIo(Event _event)
        {
            string result, deviceName = string.Empty;            

            Job job = JobTable[_event.jobIndex];
            
            // escolha do dispositivo a ser utilizado
            switch (job.Operation)
            {
                case OperationType.Read:
                    IoDevice reader = Readers.FirstOrDefault(r => r.JobIndex < 0);
                    if (reader != null)
                    {
                        job.Device = reader;
                    }
                    else 
                    {
                        job.Device = Readers.OrderBy(r => r.Queue.CurrentQueueLen).First();
                    }
                    deviceName = "leitora";
                    break;                

                case OperationType.Print:
                    IoDevice printer = Printers.FirstOrDefault(r => r.JobIndex < 0);
                    if (printer != null)
                    {
                        job.Device = printer;
                    }
                    else 
                    {
                        job.Device = Printers.OrderBy(r => r.Queue.CurrentQueueLen).First();
                    }
                    deviceName = "impressora";
                    break;                
            }

            // se dispositivo está livre, aloca e agenda próximo evento (6) para o job
            if (job.Device.JobIndex < 0)
            {
                job.Device.request(_event.jobIndex);                
                
                scheduleEvent(new Event()
                {
                    eventId = EventIdentifier.Event6Io,
                    eventTime = Clock + job.Device.ProcessingTime,
                    jobIndex = _event.jobIndex
                });
                
                result = string.Format("{0} alocado(a) e evento 6 agendado para o job {1}", deviceName, _event.jobIndex);                
            }
            // se dispositivo não está livre, job é colocado na fila
            else
            {
                job.Device.Queue.enqueue(_event.jobIndex);

                result = string.Format("job {0} colocado na fila de espera do(a) {1}\n\n{2}",
                    _event.jobIndex, deviceName, string.Format(job.Device.Queue.queueView(), deviceName));
            }

            return result;
        }

        /// <summary>
        /// Corresponde a parte do evento 5 do artigo de apoio, que foi dividido em dois pela diferenciação entre dispositivos de E/S e o disco.
        /// Trata do início do processamento de um pedido de acesso ao disco.  
        /// Marca outras partições do job que possam já estar alocadas no disco como inativas.
        /// Desaloca partições inativas caso a partição a ser alocada não caiba.
        /// No momento da alocação em si, é sempre feita uma desfragmentação.
        /// </summary>
        /// <param name="_event">Evento a ser tratado.</param>
        /// <returns>Log da execução.</returns>
        string event5RoutineDisk(Event _event)
        {
            string result;

            Job job = JobTable[_event.jobIndex];

            // marca outras partições - que não a atual - pertencentes ao mesmo job que estejam no disco como inativas
            foreach (MemoryPartition partition in Disk.PartitionTable.Where(s => s.JobIndex == _event.jobIndex 
                                                                            && s.JobPartitionIndex != job.CurrentFileIndex))
            {
                partition.Active = false;
            }

            // desaloca partições sem uso no disco caso a partição a ser alocada não caiba
            while (Disk.availableSpace < job.FileList[job.CurrentFileIndex].Size
                && Disk.PartitionTable.Any(s => s.Active == false))
            {
                Disk.deallocateFirstAllocated();
            }

            // se a partição couber no disco, faz a alocação e o job é agendado para o evento 6
            if (Disk.availableSpace >= job.FileList[job.CurrentFileIndex].Size)
            {
                Disk.allocate(_event.jobIndex);

                scheduleEvent(new Event()
                {
                    eventId = EventIdentifier.Event6Disk,
                    eventTime = Clock + Disk.ReallocationDelay,
                    jobIndex = _event.jobIndex
                });

                result = string.Format("disco alocado para arquivo {0} e evento 6 agendado para o job {1}\n\n{2}",
                    job.FileList[job.CurrentFileIndex].Name, _event.jobIndex, Disk.diskView());
            }
            // se a partição não couber no disco, o job é colocado na fila do disco
            else
            {
                Disk.Queue.enqueue(_event.jobIndex);

                result = string.Format("job {0} colocado na fila de espera do disco\n\n{1}",
                    _event.jobIndex, string.Format(CentralMemory.Queue.queueView(), "disco"));
            }

            return result;
        }

        /// <summary>
        /// Corresponde a parte do evento 6 do artigo de apoio, que foi dividido em dois pela diferenciação entre dispositivos de E/S e o disco.
        /// Trata do encerramento do processamento de um pedido de E/S.
        /// O dispositivo é liberado e o evento 3 agendado para o job.
        /// </summary>
        /// <param name="_event">Evento a ser tratado.</param>
        /// <returns>Log da execução.</returns>
        string event6RoutineIo(Event _event)
        {
            int scheduleTime = Clock + OverheadTime;

            Job job = JobTable[_event.jobIndex];
            string partialResult = job.Device.release(scheduleTime);
            job.Device = null;

            scheduleEvent(new Event()
            {
                eventId = EventIdentifier.Event3,
                eventTime = scheduleTime,
                jobIndex = _event.jobIndex
            });

            return string.Format("dispositivo liberado, evento 3 agendado para o job {0}{1}", _event.jobIndex, partialResult);
        }

        /// <summary>
        /// Corresponde a parte do evento 6 do artigo de apoio, que foi dividido em dois pela diferenciação entre dispositivos de E/S e o disco.
        /// Trata do encerramento do processamento de um pedido de acesso ao disco.
        /// O arquivo é marcado como inativo, as partições inativas são removidas do disco e o evento 3 agendado para o job.
        /// </summary>
        /// <param name="_event">Evento a ser tratado.</param>
        /// <returns>Log da execução.</returns>
        string event6RoutineDisk(Event _event)
        {
            int scheduleTime = Clock + OverheadTime;

            Job job = JobTable[_event.jobIndex];

            MemoryPartition partition = Disk.PartitionTable.FirstOrDefault(p => p.JobIndex == _event.jobIndex && p.JobPartitionIndex == job.CurrentFileIndex);
            string fileName = string.Empty;
            if (partition != null)
            {
                partition.Active = false;
                fileName = partition.FileName;
            }

            scheduleEvent(new Event()
            {
                eventId = EventIdentifier.Event3,
                eventTime = scheduleTime,
                jobIndex = _event.jobIndex
            });

            Disk.cleanUp();

            return string.Format("arquivo {0} liberado, evento 3 agendado para o job {1}", fileName, _event.jobIndex);
        }

        /// <summary>
        /// Corresponde ao evento 7 do artigo de apoio.
        /// Trata do encerramento do job completo.        
        /// O processador é liberado, todos os segmentos do job são desalocados da memória e todas as partições do job são desalocadas do disco.
        /// </summary>
        /// <param name="_event">Evento a ser tratado.</param>
        /// <returns>Log da execução.</returns>
        string event7Routine(Event _event)
        {
            StringBuilder result = new StringBuilder();

            result.AppendFormat("Job {0} finalizado, processador liberado", _event.jobIndex);
            result.Append(Cpu.release(_event.eventTime));
            result.Append(" e memoria liberada");
            result.Append(CentralMemory.deallocateAll(_event.jobIndex));
            result.Append(" e disco liberado");
            result.Append(Disk.deallocateAll(_event.jobIndex));

            return result.ToString();
        }

        /// <summary>
        /// Evento auxiliar criado para lidar com interrupções do processador.
        /// Esse evento é utilizado para controlar os limites do time slice do processador, bem como auxiliar na interrupção da execução de um job para o caso de chegada de um job de prioridade maior.
        /// Baseada no artigo de apoio, a rotina: 
        /// Remove o evento que estava agendado para o job, pois ele se torna inválido.
        /// Seta flag no job indicando que ele foi interrompido, e guarda o tempo restante de processamento.
        /// Coloca job interrompido na fila do processador.
        /// Libera o processador.
        /// </summary>
        /// <param name="_event">Evento a ser tratado.</param>
        /// <returns>Log da execução.</returns>
        string interrRoutine(Event _event)
        {                  
            // remove o evento seguinte (invalidado) para o job sendo interrompido
            Event nextEventForCurrentJob = EventList.FirstOrDefault
            (
                e => e.eventTime >= Clock
                && e.jobIndex == Cpu.JobIndex
                && (e.eventId == EventIdentifier.Event2 || e.eventId == EventIdentifier.Event4 || e.eventId == EventIdentifier.Event7)
            );
            
            EventList.Remove(nextEventForCurrentJob);            

            // seta propriedades do job interrompido
            Job currentJob = JobTable[Cpu.JobIndex];
            currentJob.Interrupted = true;
            currentJob.InterruptedRemainingTime = nextEventForCurrentJob.eventTime - Clock;

            // coloca o job interrompido na fila do processador
            Cpu.Queue.enqueue(JobTable.IndexOf(currentJob));

            // libera o processador
            string partialResult = Cpu.release(Clock);            

            return string.Format("job {0} interrompido{1}", JobTable.IndexOf(currentJob), partialResult);
        }
    }
}
