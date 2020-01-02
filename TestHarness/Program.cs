using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.MetadirectoryServices;
using FimSync_Ezma;
using System;
using System.Configuration;

namespace TestHarness
{
    class configCollection : KeyedCollection<string, ConfigParameter>
    {
        protected override string GetKeyForItem(ConfigParameter item)
        {
            return item.Name;
        }
    }
    class Program
    {
        #region constants
        private const string  _LANG = "en-GB";
        private const string _RESTBASEURL = "https://api.themoviedb.org/3/";
        private const string _LIMITRTNPAGES = "1";
        #endregion

        private static readonly string _APIKEY = ConfigurationManager.AppSettings["APIKey"];

        static void Main(string[] args)
        {
            List<dynamic> resultsList = new List<dynamic>();
            FimSync_Ezma.EzmaExtension tester = new EzmaExtension();
            configCollection foo = new configCollection();

            ConfigParameter cpBaseUri = new ConfigParameter("Base URI", _RESTBASEURL);
            ConfigParameter cpApi = new ConfigParameter("API Key", _APIKEY);
            ConfigParameter cpLang = new ConfigParameter("Language", _LANG);
            ConfigParameter cpLIMIT = new  ConfigParameter("# of Pages to Return", _LIMITRTNPAGES);

            foo.Add(cpBaseUri);
            foo.Add(cpApi);
            foo.Add(cpLang);
            foo.Add(cpLIMIT);

            tester.GetSchema(foo);
            tester.OpenImportConnection(foo, new Schema(), new OpenImportConnectionRunStep());
            tester.GetImportEntries(new GetImportEntriesRunStep());
            tester.CloseImportConnection(new CloseImportConnectionRunStep());
            Console.WriteLine("Press any key to stop...");
            Console.ReadKey();
        }
    }
}