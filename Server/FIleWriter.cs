using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
   public class FileWriter : IDisposable
    {
        private StreamWriter writer;
        private bool disposed = false;

        public string FilePath { get; }

        public FileWriter(string filePath)
        {
            FilePath = filePath;
            writer = new StreamWriter(filePath, append: true);
        }

        public void WriteLine(string line)
        {
            if (disposed)
                throw new ObjectDisposedException("FileWriter");

            writer.WriteLine(line);
            writer.Flush();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    writer?.Close();
                    writer?.Dispose();
                }
                disposed = true;
            }
        }

        ~FileWriter()
        {
            Dispose(false);
        }
    }
}
