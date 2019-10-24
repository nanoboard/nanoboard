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
            //Console.WriteLine(DateTime.Now.ToString("R"));
			//Console.WriteLine("Program.cs: Aggregator.CheckUpdatePlacesConfig();");
            Aggregator.CheckUpdatePlacesConfig();			//Update, if places was been updated, and contains in places.txt
			//Console.WriteLine("Program.cs: Aggregator.CheckUpdateProxyList();");
            Aggregator.CheckUpdateProxyList();				//Update, if proxies was been updated, and contains in proxy.txt
			//Console.WriteLine("Program.cs: Aggregator.CheckUpdateIPServicesConfig();");
            Aggregator.CheckUpdateIPServicesConfig();		//Update, if IP_services was been updated in the "Settings" or externalIPservices.txt. See /pages/params.html
            var db = new PostDb();
            nbpack.NBPackMain.PostDatabase = db;
            var serv = new HttpServerBuilder(db).Build();
            serv.Run();            
        }
    }
}
