using System.Collections.Generic;

namespace HangAnalyzer
{
    internal class CallStack
    {
        public int ID { get; }
        public readonly List<string> Frames = new List<string>();

        public CallStack(int id)
        {
            this.ID = id;
        }

        internal void AddFrame(string frame)
        {
            Frames.Add(frame);
        }

        internal bool Matches(string criteria)
        {
            foreach (var frame in Frames)
                if (frame.Contains(criteria))
                    return true;
            return false;
        }
    }
}