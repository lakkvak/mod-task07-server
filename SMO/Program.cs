using System;
using System.Threading;
using System.IO;

namespace SMO
{
    public class procEventArgs : EventArgs
    {
        public int id { get; set; }
    }
    struct PoolRecord
    {
        public Thread thread;
        public bool in_use;
        public int wait;
        public int work;
    }
    class Server
    {
        public int requestCount;
        public int processedCount;
        public int rejectedCount;
        public int poolcount;

        public PoolRecord[] pool;
        object threadLock;

        public Server(int count, PoolRecord[] pool)
        {

            processedCount = 0;
            requestCount = 0;
            this.pool = pool;
            this.poolcount = count;
            threadLock = new object();
            for (int i = 0; i < poolcount; i++)
                pool[i].in_use = false;
        }
        private void Answer(object e)
        {
            Console.WriteLine("Выполняется заявка с номером {0}", e);
            Thread.Sleep(10);
            Console.WriteLine("Заявка с номером {0} выполнена", e);
            for (int i = 0; i < poolcount; i++)
            {
                if (pool[i].thread == Thread.CurrentThread)
                {
                    pool[i].in_use = false;
                    pool[i].thread = null;
                    break;
                }
            }
        }
        public void proc(object sender, procEventArgs e)
        {
            lock (threadLock)
            {
                Console.WriteLine("Заявка с номером {0}", e.id);
                requestCount++;
                for (int i = 0; i < poolcount; i++)
                {
                    if (!pool[i].in_use)
                        pool[i].wait++;
                }
                for (int i = 0; i < poolcount; i++)
                {
                    if (!pool[i].in_use)
                    {
                        pool[i].work++;
                        pool[i].in_use = true;
                        pool[i].thread = new Thread(new ParameterizedThreadStart(Answer));
                        pool[i].thread.Start(e.id);
                        processedCount++;
                        return;
                    }
                }
                rejectedCount++;
            }
        }
        private long Fact(long n)
        {
            if (n == 0)
                return 1;
            else
                return n * Fact(n - 1);
        }
        public void Report()
        {


            double p = requestCount / poolcount;
            double temp = 0;
            for (int i = 0; i < poolcount; i++)
                temp += Math.Pow(p, i) / Fact(i);
            double p0 = 1 / temp;
            double pn = Math.Pow(p, poolcount) * p0 / Fact(poolcount);
            Console.WriteLine("\n");
            Console.WriteLine("Количество потоков: " + poolcount + '\n' + "Всего запросов: " + requestCount + '\n' + "Выполнено запросов: " + processedCount + '\n' + "Отклонено запросов: " + rejectedCount);
            for (int i = 0; i < poolcount; i++)
                Console.WriteLine("Потоком с номером " + (i + 1) + " выполнено запросов: " + pool[i].work + "; тактов ожидания: " + pool[i].wait);
            Console.WriteLine("Интенсивность потока заявок: " + p);
            Console.WriteLine("Вероятность простоя системы: " + p0);
            Console.WriteLine("Вероятность отказа системы: " + pn);
            Console.WriteLine("Относительная пропускная способность: " + (1 - pn));
            Console.WriteLine("Абсолютная пропускная способность: " + (requestCount * (1 - pn)));
            Console.WriteLine("Среднее число занятых каналов: " + ((requestCount * (1 - pn)) / poolcount));


        }
    }
    class Client
    {
        public event EventHandler<procEventArgs> request;
        Server server;

        int index = 0;

        public Client(Server server)
        {
            this.server = server;
            this.request += server.proc;
            index = 0;
        }
        protected virtual void OnProc(procEventArgs e)
        {
            EventHandler<procEventArgs> handler = request;
            if (handler != null)
                handler(this, e);
        }
        public void Work()
        {
            procEventArgs e = new procEventArgs();
            index++;
            e.id = index;
            this.OnProc(e);
        }
    }
    class Program
    {

        static int threadCount = 10;
        static int requestCount = 80;
        static PoolRecord[] pool = new PoolRecord[threadCount];

        static void Main(string[] args)
        {
            Server server = new Server(threadCount, pool);
            Client client = new Client(server);
            for (int i = 0; i < requestCount; i++)
                client.Work();
            Thread.Sleep(1000);
            server.Report();


        }
    }
}