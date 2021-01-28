namespace HangAnalyzer
{
    public class DotNetThread
    {
        public int ID { get; }
        public bool GCEnabled { get; }
        public bool Finalizer { get; }
        public bool GC { get; }
        public bool Exception { get; }

        public DotNetThread(string line)
        {
            var tokens = line.Split(' ');
            if (tokens[2] != "")
                ID = int.Parse(tokens[2]);
            else if (tokens[3] != "")
                ID = int.Parse(tokens[3]);
            if (line.Contains("GC")) GC = true;
            if (line.Contains("Finalizer")) Finalizer = true;
            if (line.Contains("Exception")) Exception = true;
            if (line.Contains("Enabled")) GCEnabled = true;
        }
    }
}