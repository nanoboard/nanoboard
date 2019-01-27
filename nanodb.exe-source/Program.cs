using System;
using NDB;
using System.Linq;
using NServer;
using nboard;

namespace NDB
{
    class MainClass
    {
        /*
            Application's entry point. 
            Builds server, assigns new DB instance to it and runs it;
        */
        public static void Main(string[] args)
        {
            Aggregator.CheckUpdatePlacesConfig();
            var db = new PostDb();
            nbpack.NBPackMain.PostDatabase = db;
            var serv = new HttpServerBuilder(db).Build();
            serv.Run();
        }
    }
}
