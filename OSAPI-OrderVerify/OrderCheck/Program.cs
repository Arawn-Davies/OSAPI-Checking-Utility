using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Collections;

namespace OrderCheck
{
    internal class Program
    {
        static void Main(string[] args)
        {

            if (args.Length != 3)
            {
                Console.WriteLine("Incorrect number of command line arguments used. Aborting...");
                Environment.Exit(1);
            }

            // OS API BigTIFF data directory, data package ID, API Key

            //string path = @"C:\WhereToDownloadTheTIFFs\";
            //string datapackage_id = "DataPackageID";
            //string apikey = @"YourAPIKeyGoesHere";

            string path = args[0];
            string datapackage_id = args[1];
            string apikey = args[2];

            string ver = "";

            // Empty array holding the order details file lines
            // This array gets processed to retrieve the expected files
            string[] orderdetails = Array.Empty<string>();

            // Array holding each file in the data directory
            string[] orderfiles = Directory.GetFiles(path);

            // Scans the data directory for a text file. OSAPI Download only has one text file in a data directory. This is always the order manifest. 
            // Will fail if there is a second file with a similar name (containing -Order_Details.txt)
            foreach (string file in orderfiles)
            {
                if (file.EndsWith("-Order_Details.txt"))
                {
                    string fName = Path.GetFileName(file);
                    ver = fName.Split('-')[0];
                    Console.WriteLine("Version: " + ver);
                    orderdetails = File.ReadAllLines(file);
                }
            }

            // Declare + initialize empty lists, holding either the order's expected files, the actual files that were downloaded and optionally any missing files
            List<string> exp_files = new List<string>();
            List<string> acc_files = new List<string>();
            List<string> non_files = new List<string>();


            // Scan the order details for the expected files            
            foreach (string line in orderdetails)
            {
                if (line.StartsWith("                  ") && line.EndsWith(".tif"))
                {
                    string expected_file = line.Replace("                  ", "");
                    exp_files.Add(expected_file);
                }
            }

            // Scan the data directory for the TIF files that were actually downloaded
            foreach (string file in orderfiles)
            {
                if (file.EndsWith(".tif"))
                {
                    string fileName = Path.GetFileName(file);
                    acc_files.Add(fileName);
                }
            }

            // Compare the expected files with the actual files, identify any missing files
            foreach (string file in exp_files)
            {
                if (!(acc_files.Contains(file)))
                {
                    Console.WriteLine("Missing file: " + file);
                    non_files.Add(file);
                }
            }

            Console.WriteLine("Actual file count: " + acc_files.Count);
            Console.WriteLine("Expected file count: " + exp_files.Count);

            // Compare file counts, exit accordingly
            if (exp_files.Count == acc_files.Count)
            {
                Console.WriteLine("All files downloaded correctly!");
                Environment.Exit(0);
            }
            else
            {
                Console.WriteLine("Some files have failed to be downloaded. Please check above to see which file");
            }


            // Redownload missing TIFFs from Ordnance Survey

            string url = "";

            string osapi_url_part1 = @"https://api.os.uk/downloads/v1/dataPackages/" + datapackage_id;
            string osapi_url_part2 = "/versions/" + ver + "/downloads?fileName=";
            string osapi_url_part3 = "&key=" + apikey;



            foreach (string file in non_files)
            {
                url = osapi_url_part1 + osapi_url_part2 + file + osapi_url_part3;
                byte[] tif_dl = Array.Empty<byte>();
                using (HttpClient dlClient = new HttpClient())
                {
                    dlClient.DefaultRequestHeaders.Add("key", apikey);
                    dlClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue(@"*/*"));
                    Console.WriteLine("Downloading file " + url + " to " + path + file + "\n");
                    var resp = dlClient.GetAsync(url).GetAwaiter().GetResult();
                    if (resp.IsSuccessStatusCode)
                    {
                        var responseContent = resp.Content;
                        tif_dl = responseContent.ReadAsByteArrayAsync().GetAwaiter().GetResult();
                        File.WriteAllBytes(path + file, tif_dl);
                    }
                    else
                    {
                        Console.WriteLine("Error: " + resp.StatusCode);
                    }
                    
                }
            }

            acc_files.Clear();

            orderfiles = Directory.GetFiles(path);


            foreach (string file in orderfiles)
            {
                if (file.EndsWith(".tif"))
                {
                    string fileName = Path.GetFileName(file);
                    acc_files.Add(fileName);
                }
            }

            // Compare the expected files with the actual files, identify any missing files
            foreach (string file in exp_files)
            {
                if (!(acc_files.Contains(file)))
                {
                    Console.WriteLine("Missing file: " + file);
                }
            }

            Console.WriteLine("Actual file count: " + acc_files.Count);
            Console.WriteLine("Expected file count: " + exp_files.Count);

            // Compare file counts, exit accordingly
            if (exp_files.Count == acc_files.Count)
            {
                Console.WriteLine("All files downloaded correctly!");
                Environment.Exit(0);
            }
            else
            {
                Environment.Exit(1);
            }

        }
    }
}
