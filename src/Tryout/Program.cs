using EventStore.CommonDomain.Dispatcher;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Tryout
{
    class Program
    {
        static void Main(string[] args)
        {
            var mre = new ManualResetEvent(false);

            var connection = new EventStore.ClientAPI.EventStoreConnection(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 1113));


            var pub = new Event_Publisher(connection, "storeId");
            pub.Reset();
            pub.Start();

            Console.ReadLine();
            //mre.WaitOne();
            return;

            try
            {

                connection.SubscribeToAllStreamsAsync((ev, pos) =>
                {
                    Console.WriteLine("{0} : {1}, {2}",ev.EventNumber, ev.EventType, ev.EventStreamId);
                    Console.ReadLine();
                }, () => { });

                mre.WaitOne();

            }
            catch (Exception ex)
            {
            }
        }
    }
}
