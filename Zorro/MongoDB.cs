using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using static Zorro.Classes;
using static Zorro.WebTools;

namespace Zorro
{
    class MongoDB
    {

        public static MongoClient Client;
        public static IMongoDatabase Database;
        public static IMongoCollection<Entry> CommonCollection;

        public static void PushBatch(List<Entry> Entries)
        {
            CheckConnection();
            var CollectionString = Entries.First().Collection;
            var Collection = CommonCollection = Database.GetCollection<Entry>(CollectionString);

            foreach (var Entry in Entries)
            {
                Entry._id = CreateMD5(Entry.Link).ToString();
            }
            bool Done = false;
            while (!Done)
            {
                try
                {
                    if (Entries.Any())
                        Collection.InsertMany(Entries, new InsertManyOptions() { IsOrdered = false });
                    Done = true;
                }
                catch (MongoBulkWriteException<Zorro.Classes.Entry> ex)
                {
                    
                    foreach (var Error in ex.WriteErrors)
                    {
                        Entries[Error.Index] = null;
                    }
                    Entries.RemoveAll(x => x == null);
                }
            }
        }

        public static async void SendToMongo(Entry Entry, bool DoCheckConnection = true)
        {
            if (DoCheckConnection)
            {
                CheckConnection();
            }

            if (!string.IsNullOrWhiteSpace(Entry.Collection))
            {
                var iCollection = Database.GetCollection<Entry>(Entry.Collection);
                await iCollection.InsertOneAsync(Entry);
            }
            else
            {
                await CommonCollection.InsertOneAsync(Entry);
            }
        }

        public static IMongoCollection<Stat> StatCollection;
        public static async void SendStat(Stat Stat)
        {
            if (Client == null)
                CheckConnection();

            if(StatCollection == null)
                StatCollection = Database.GetCollection<Stat>("Stats");

            await StatCollection.InsertOneAsync(Stat);
        }

        public static void CheckConnection()
        {
            bool Connected = false;
            try
            {
                var PingResponse = JsonConvert.DeserializeObject<dynamic>(Database.RunCommandAsync((Command<BsonDocument>)"{ping:1}").Result.ToString());
                if (PingResponse.ok == 1)
                    Connected = true;
            }
            catch { }

            if (!Connected)
            {
                Client = new MongoClient(File.ReadAllText("mongocon"));
                Database = Client.GetDatabase("Zorro");
                CommonCollection = Database.GetCollection<Entry>("Common");
            }
        }

        public static string CreateMD5(string input)
        {
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2"));
                }
                return sb.ToString();
            }
        }

    }
}
