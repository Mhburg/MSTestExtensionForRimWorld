using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using RPG_Inventory_Remake_Common.UnitTest;

namespace UnitTestAdapterForRW
{
    

    [DefaultExecutorUri(RWConstants.ExecutorUri)]
    public class RWTestDiscoverer : ITestDiscoverer
    {
        public void DiscoverTests(IEnumerable<string> sources, IDiscoveryContext discoveryContext, IMessageLogger logger, ITestCaseDiscoverySink discoverySink)
        {
            if (sources == null)
                throw new ArgumentNullException(nameof(sources));
            if (discoverySink == null)
            {
                throw new ArgumentNullException(nameof(discoverySink));
            }

#if DEBUG
            string path = @"D:\Modding\UnitTestAdapterForRW\UnitTestAdapterForRW\bin\Debug\TestSources.txt";
            // Create a file to write to.
            StreamWriter sw = File.CreateText(path);
            sw.WriteLine("Write from Discoverer");
#endif

            foreach (string source in sources)
            {
                Assembly assembly = Assembly.LoadFrom(source);
                DiaSession diaSession = new DiaSession(source);
                foreach (Type type in assembly.GetTypes())
                {
                    if (typeof(RPGIUnitTest).IsAssignableFrom(type))
                    {
                        var diaNavigationData = diaSession.GetNavigationDataForMethod(type.FullName, "Run")
                                             ?? diaSession.GetNavigationDataForMethod(type.FullName, "Setup");
                        try
                        {
                            if (diaNavigationData == null)
                            {
                                sw.WriteLine("Naviagation data is null");
                            }
                            else
                            {
                                sw.WriteLine("Code file path: " + diaNavigationData.FileName);
                                sw.WriteLine("LineNumber: " + diaNavigationData.MinLineNumber);
                            }
                        }
                        catch (Exception e)
                        {
                            sw.WriteLine(e.Message + e.StackTrace);
                        }
                        discoverySink.SendTestCase(new TestCase(type.FullName, new Uri(RWConstants.ExecutorUri), source)
                        {
                            CodeFilePath = diaNavigationData.FileName ?? string.Empty,
                            LineNumber = diaNavigationData?.MinLineNumber ?? 0
                        });
                        sw.WriteLine(type.FullName);
                    }
                }
                sw.WriteLine(source);
            }
            sw.Dispose();
        }
    }
}
