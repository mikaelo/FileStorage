using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApplication1
{
    class Program
    {
        static void Main(string[] args)
        {
            
            string file1 = "file1_1.pdf";
            string file2 = "file1_2.pdf";

            //CompareTwoFiles(file1, file2).Wait();
            Load100Files("http://localhost:9999/api/CardStatement").Wait();

            //Load100Files2("http://localhost:9999/api/CardStatement");
            Console.ReadLine();
        }


        private static async void  Load100Files2(string url)
        {
            var taskCount = 10;

            try
            {
                var tasks = new List<Task>();

                var numberCores = Environment.ProcessorCount;
                int fileNameIndex = 0;
                for (int i = 0; i < numberCores; i++)
                {
                    tasks.Add(LoadFile(url, "file" + (fileNameIndex++) + ".pdf"));
                }

                while (tasks.Count > 0)
                {
                   var task = await Task.WhenAny(tasks.ToArray());
                    if (task.IsFaulted)
                    {
                        Console.WriteLine(task.Exception.InnerException.Message);
                    }

                    tasks.Remove(task);

                    taskCount--;

                    if (taskCount > 0)
                    {
                        tasks.Add(LoadFile(url, "file" + (fileNameIndex++) + ".pdf"));
                    }
                }
            }

            catch (AggregateException ae)
            {

                var exceptions = ae.Flatten();
                foreach (var exception in exceptions.InnerExceptions)
                {
                    Console.WriteLine(exception.Message);
                }

                return;
            }


            for (int i = 1; i <= taskCount; i++)
            {
                var file1 = "file" + (i - 1) + ".pdf";
                var file2 = "file" + i % taskCount + ".pdf";
                Console.WriteLine("Compare '{0}' to '{1}' is {2}", file1, file2, CompareFiles(file1, file2));
            }

        }

        static async Task Load100Files(string url)
        {
            var numberOfTasks = 500;

            IEnumerable<Task> tasksQuery = from i in Enumerable.Range(0, numberOfTasks) select LoadFile(url, "file" + i + ".pdf");
            var tasks = tasksQuery.ToArray();
            try
            {
                await Task.WhenAll(tasks);
                //var completeTasks = Task.WhenAll(tasks);
                //await completeTasks.ContinueWith(x => { });
            }
            catch (Exception ex)
            {
                foreach (Task faulted in tasks.Where(t => t.IsFaulted))
                {
                    Console.WriteLine(faulted.Exception.InnerException);
                }

                return;
            }
  

            for (int i = 1; i <= numberOfTasks; i++)
            {
                var file1 = "file" + (i - 1) + ".pdf";
                var file2 = "file" + i % numberOfTasks + ".pdf";
                Console.WriteLine("Compare '{0}' to '{1}' is {2}", file1, file2, CompareFiles(file1, file2));
            }

        }
        static async Task CompareTwoFiles(string file1, string file2)
        {
            var task1 = LoadFile("https://localhost:8001/Home/File", file1);
            var task2 = LoadFile("http://localhost:9999/api/CardStatement", file2);

            var tasks = Task.WhenAll(task1, task2);
            await tasks.ContinueWith(x => { });

            Console.WriteLine("Compare '{0}' to '{1}' is {2}", file1, file2, CompareFiles(file1, file2));

        }
        static async Task LoadFile(string url, string fileName)
        {
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.ServerCertificateValidationCallback = delegate { return true; };
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.Timeout = 10 * 1000;

            var requestBody = string.Format("login={0}&password={1}&guid={2}&agid={3}&abstract_period={4}",
                "pdf", "", "", "", "");

            var content = Encoding.UTF8.GetBytes(requestBody);

            using (var requestStream = await request.GetRequestStreamAsync())
            {
                await requestStream.WriteAsync(content, 0, content.Length);
            }

            using(var response = await request.GetResponseAsync())
            using (var responseStream = response.GetResponseStream())
            {
                //var fileName = response.Headers["Content-Disposition"].Substring(response.Headers["Content-Disposition"].IndexOf("filename=") + 9).Replace("\"", "");
                using (var fileStream = File.Create(fileName))
                {
                    await responseStream.CopyToAsync(fileStream);
                }

            }
        }

        // This method accepts two strings the represent two files to 
        // compare. A return value of 0 indicates that the contents of the files
        // are the same. A return value of any other value indicates that the 
        // files are not the same.
        private static bool CompareFiles(string file1, string file2)
        {
            int file1byte;
            int file2byte;
            FileStream fs1;
            FileStream fs2;

            // Determine if the same file was referenced two times.
            if (file1 == file2)
            {
                // Return true to indicate that the files are the same.
                return true;
            }

            // Open the two files.
            fs1 = new FileStream(file1, FileMode.Open, FileAccess.Read);
            fs2 = new FileStream(file2, FileMode.Open, FileAccess.Read);

            // Check the file sizes. If they are not the same, the files 
            // are not the same.
            if (fs1.Length != fs2.Length)
            {
                // Close the file
                fs1.Close();
                fs2.Close();

                // Return false to indicate files are different
                return false;
            }

            // Read and compare a byte from each file until either a
            // non-matching set of bytes is found or until the end of
            // file1 is reached.
            do
            {
                // Read one byte from each file.
                file1byte = fs1.ReadByte();
                file2byte = fs2.ReadByte();
            }
            while ((file1byte == file2byte) && (file1byte != -1));

            // Close the files.
            fs1.Close();
            fs2.Close();

            // Return the success of the comparison. "file1byte" is 
            // equal to "file2byte" at this point only if the files are 
            // the same.
            return ((file1byte - file2byte) == 0);
        }

        /// <summary>
		/// Given a list of Tasks, waits for them all to finish --- but optimizes by processing them
		/// as soon as they finish (vs. Task.WaitAll which waits in the order they appear in the
		/// array).
		/// </summary>
		/// <param name="tasks"></param>
		private static void WaitAllOneByOne(List<Task> tasks)
		{
			AggregateException ae = null;

			//
			// wait for all the tasks in the list to finish:
			//
			while (tasks.Count > 0)
			{
				// wait for whomever finishes next:
				int i = Task.WaitAny(tasks.ToArray());

				// did task fail?
				if (tasks[i].Exception != null)  // yes:
				{
					// keep a running exception object (forms a tree if they are multiple failures):
					ae = (ae == null) ? 
						new AggregateException("A task failed, see inner exception(s)...", tasks[i].Exception) :
						new AggregateException("A task failed, see inner exception(s)...", tasks[i].Exception, ae);
				}

				tasks.RemoveAt(i);
			}//while

			//
			// done: if any of the tasks failed, throw an exception:
			//
			if (ae != null)
				throw ae;
		}

        /// <summary>
        /// Given a list of Tasks, waits for them all to finish --- but optimizes by processing them
        /// as soon as they finish (vs. Task.WaitAll which waits in the order they appear in the
        /// array).
        /// </summary>
        /// <param name="tasks"></param>
        private static void WaitAllOneByOne2(List<Task> tasks)
        {
            AggregateException ae = null;

            //
            // wait for all the tasks in the list to finish:
            //
            while (tasks.Count > 0)
            {
                // wait for whomever finishes next:
                int i = Task.WaitAny(tasks.ToArray());

                // did task fail?
                if (tasks[i].Exception != null)  // yes:
                {
                    // keep a running exception object (forms a tree if they are multiple failures):
                    ae = (ae == null) ?
                        new AggregateException("A task failed, see inner exception(s)...", tasks[i].Exception) :
                        new AggregateException("A task failed, see inner exception(s)...", tasks[i].Exception, ae);
                }

                tasks.RemoveAt(i);
            }//while

            //
            // done: if any of the tasks failed, throw an exception:
            //
            if (ae != null)
                throw ae;
        }
    }
}
