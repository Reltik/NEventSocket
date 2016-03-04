﻿namespace NEventSocket.Examples.Examples
{
    using System;
    using System.Reactive.Linq;
    using System.Reactive.Threading.Tasks;
    using System.Threading;
    using System.Threading.Tasks;

    using ColoredConsole;

    using Net.CommandLine;
    using Net.System;

    using NEventSocket.FreeSwitch;
    using NEventSocket.Util;

    public class LoadTest : ICommandLineTask, IDisposable
    {
        private readonly CommandLineReader commandLineReader;

        public LoadTest(CommandLineReader commandLineReader)
        {
            this.commandLineReader = commandLineReader;
        }

        public Task Run(CancellationToken cancellationToken)
        {
            int maxThreads;
            int maxIoPorts;
            //System.Threading.ThreadPool.GetMaxThreads(out maxThreads, out maxIoPorts);
            //System.Threading.ThreadPool.SetMaxThreads(maxThreads * 2, maxIoPorts * 2);

            int authFailures = 0;
            int activeClients = 0;
            int heartbeatsReceived = 0;
            int maxClients = 300;
            
            maxClients = commandLineReader.ReadObject<int>(cancellationToken);
            
            Parallel.For(0, maxClients,
                async (_) =>
                {
                    long clientId = 0;
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    try
                    {
                        using (InboundSocket client = await InboundSocket.Connect("127.0.0.1", 8021, "ClueCon", TimeSpan.FromSeconds(10)))
                        {
                            clientId = client.Id;
                            await client.SubscribeEvents(EventName.Heartbeat);

                            EventMessage heartbeat =
                                await client.Events.FirstOrDefaultAsync(x => x.EventName == EventName.Heartbeat).ToTask(cancellationToken);
                            if (heartbeat != null)
                            {
                                Interlocked.Increment(ref heartbeatsReceived);
                                ColorConsole.WriteLine("Client ".DarkCyan(), clientId.ToString(), " reporting in ".DarkCyan());
                            }
                        }
                    }
                    catch (TimeoutException)
                    {
                        ColorConsole.WriteLine("Auth timeout Client id:".OnDarkRed(), clientId.ToString().Red());
                        Interlocked.Increment(ref authFailures);
                    }
                    catch (TaskCanceledException)
                    {
                        
                    }
                });

            ColorConsole.WriteLine("Press [Enter] to exit.".Green());
            Console.ReadLine();

            ColorConsole.WriteLine("THere were {0} heartbeats".Fmt(heartbeatsReceived).Green());
            ColorConsole.WriteLine("THere were {0} failures".Fmt(authFailures).Red());

            return Task.FromResult(0);
        }

        public void Dispose()
        {
        }
    }
}