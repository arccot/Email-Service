using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.Data.SQLite;
using System.Configuration;
using System.IO;
using EmailLibrary;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Email_API
{

    public class MyTask<T, B> {
        public T result { get; set; }
        public B data { get; set; }
    }
    public class EmailRepository
    {
        private static ConcurrentQueue<Email> writeQueue = new ConcurrentQueue<Email>();
        private static ConcurrentQueue<MyTask<string, int?>> readQueue = new ConcurrentQueue<MyTask<string, int?>>();
        private static ConcurrentQueue<Email> emailQueue = new ConcurrentQueue<Email>();
        private static Boolean alive = true;
        private static List<Thread> tasks = new List<Thread>();
        private static string ConnectionString;
        private static string newLine = System.Environment.NewLine;
        public static void InitializeDatabase()
        {
            ConnectionString = ConfigurationManager.ConnectionStrings["conn"].ToString() + ";PRAGMA journal_mode=WAL;";
            var dbPath = ConnectionString.Split(';')[0].TrimStart("Data Source=".ToCharArray());
            if (!File.Exists(dbPath))
            {
                SQLiteConnection.CreateFile(dbPath);
            }
            CreateTable();
            var b = new Thread(readQueueTask);
            var a = new Thread(runBulkAddTask);
            tasks.Add(a);
            a.Start();
            tasks.Add(b);
            b.Start();
            for (int i = 0; i < Math.Max(1, Environment.ProcessorCount - 2); i++)
            {
                var c = new Thread(emailProcessor);
                tasks.Add(c);
                c.Start();
            }
        }

        public static void emailProcessor()
        {
            while (alive)
            {
                if (!emailQueue.IsEmpty)
                {
                    Email data;
                    emailQueue.TryDequeue(out data);
                    while (data != null)
                    {
                        AddEmail(data);
                    }
                }
                Thread.Sleep(500);
            }
        }

        public static void readQueueTask()
        {
            var Conn = makeConnection();
            var command = Conn.CreateCommand();
            command.CommandText = "SELECT * FROM emails";

            while (alive)
            {
                var list = "";
                var init = false;
                MyTask<string, int?> res;
                readQueue.TryDequeue(out res);
                while (res != null)
                {
                    if (!init)
                    {
                        var reader = command.ExecuteReader();
                        using (reader)
                        {
                            while (reader.Read())
                            {
                                list += $"{reader["recipient"]}, {reader["sender"]}, {reader["subject"]}, {reader["body"]}, {reader["date"]}, {reader["delivered"]}{newLine}";
                            }
                        }
                        init = true;
                    }
                    res.result = list;
                    res.data = 0;
                    readQueue.TryDequeue(out res);
                }
                Thread.Sleep(1000);
            }
        }

        public static void runBulkAddTask()
        {
            var Conn = makeConnection();
            var command = Conn.CreateCommand();
            command.CommandText = @"INSERT INTO emails (recipient, sender, subject, body, date, delivered) VALUES ($rec, $sen, $sub, $bod, $t, $del)";
            var rec = command.CreateParameter();
            rec.ParameterName = "$rec";
            command.Parameters.Add(rec);
            var sen = command.CreateParameter();
            sen.ParameterName = "$sen";
            command.Parameters.Add(sen);
            var sub = command.CreateParameter();
            sub.ParameterName = "$sub";
            command.Parameters.Add(sub);
            var bod = command.CreateParameter();
            bod.ParameterName = "$bod";
            command.Parameters.Add(bod);
            var t = command.CreateParameter();
            t.ParameterName = "$t";
            command.Parameters.Add(t);
            var del = command.CreateParameter();
            del.ParameterName = "$del";
            command.Parameters.Add(del);

            var epox = new DateTime(1970, 1, 1);
            while (alive)
            {
                if (!writeQueue.IsEmpty)
                {
                    using (var transaction = Conn.BeginTransaction())
                    {

                        Email data;

                        writeQueue.TryDequeue(out data);
                        while (data != null)
                        {
                            rec.Value = data.Recipient;
                            sen.Value = data.Sender;
                            sub.Value = data.Subject;
                            bod.Value = data.Body;
                            t.Value = data.Date.Subtract(epox).TotalSeconds;
                            del.Value = data.Delivered;
                            command.ExecuteNonQuery();
                            writeQueue.TryDequeue(out data);
                        }

                        transaction.Commit();
                    }
                }
                Thread.Sleep(100);
            }
        }

        public static void SendEmail(Email email)
        {
            emailQueue.Enqueue(email);
        }

        public static void AddEmail(Email email)
        {
            email.Send();
            writeQueue.Enqueue(email);
        }
        public static string GetEmails()
        {
            var task = new MyTask<string, int?>();
            readQueue.Enqueue(task);
            while (task.data == null)
            {
                Thread.Sleep(100);
            }
            return task.result;
        }

        public static SQLiteConnection makeConnection()
        {
            var Conn = new SQLiteConnection(ConnectionString);
            Conn.Open();
            return Conn;
        }
        public static void CreateTable()
        {
            var Conn = makeConnection();
            using (Conn)
            {
                using (var cmd = Conn.CreateCommand())
                {
                    cmd.CommandText = $"CREATE TABLE IF NOT EXISTS emails (id INTEGER PRIMARY KEY AUTOINCREMENT, recipient TEXT NOT NULL, sender TEXT NOT NULL, subject TEXT, body TEXT, date INTEGER, delivered INTEGER NOT NULL)";
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}