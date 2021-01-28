using System;
using System.Collections.Generic;
using System.Linq;

namespace HangAnalyzer
{
    public class Analyzer
    {
        readonly List<CallStack> NativeStacks = new List<CallStack>();
        readonly List<CallStack> DotNetStacks = new List<CallStack>();
        readonly List<DotNetThread> DotNetThreads = new List<DotNetThread>();

        public Analyzer(string[] lines)
        {
            ParseInput(lines);
        }

        public void Analyze()
        {
            PrintHeader("Hang Analysis");
            AnalyzeGC();
            AnalyzeLocks();
            AnalyzeWaitingForExternal();
            PrintHangPosts();
        }

        private void AnalyzeWaitingForExternal()
        {
            PrintH2("External Resources");
            AnalyzeSocketReceive();
            AnalyzeSendReceive2();
            AnalyzeGetToSTA();
        }

        private void AnalyzeGetToSTA()
        {
            var waitingToGetToSTA = ThreadsMatching(NativeStacks, "GetToSTA");
            if(waitingToGetToSTA != "")
                Console.WriteLine($"The following threads are waiting in a GetToSTA (Waiting for an STA thread):\t(TIP! Run !sieextPub.comcalls to find out where the calls are going)\n\t{waitingToGetToSTA}\n");
        }

        private void AnalyzeSendReceive2()
        {
            var waitingForSendReceive2 = ThreadsMatching(NativeStacks, "SendReceive2");
            if (waitingForSendReceive2 != "")
                Console.WriteLine($"The following threads are waiting in a SendReceive2 (COM Call):\t(TIP! Run !sieextPub.comcalls to find out where the calls are going)\n\t{waitingForSendReceive2}\n");
        }

        private void AnalyzeSocketReceive()
        {
            var waitingInSocketReceive = ThreadsMatching(DotNetStacks, "Socket.Receive");
            if (waitingInSocketReceive != "")
                Console.WriteLine($"The following threads are waiting in a Socket.Receive: \n\t{waitingInSocketReceive}\n");
        }

        private void PrintHangPosts()
        {
            Console.WriteLine("\nBLOG POSTS ABOUT DEBUGGING HANGS");
            Console.WriteLine("==============================================");
            Console.WriteLine("Hang debugging walkthrough\t\t https://tessferrandez.github.io/debugging/dotnet/hang/2006/10/13/net-hang-debugging-walkthrough.html");
            Console.WriteLine("Things to ignore when you're debugging a hang\t https://tessferrandez.github.io/debugging/aspnet/hang/2007/04/02/things-to-ignore-when-debugging-a-net-hang.html\n\n\n\n");
        }

        private void AnalyzeLocks()
        {
            PrintH2("Locks and Critical Sections");
            AnalyzeThreadsWaitingToEnterLock();
            AnalyzeThreadsWaitingToEnterManagedLock();
            AnalyzeThreadsWaitingForCriticalSection();
            AnalyzeThreadsWaitingInWaitOne();
            AnalyzeThreadsWaitingInWaitMultiple();
            PrintLockPosts();
        }

        private void AnalyzeThreadsWaitingInWaitMultiple()
        {
            string waitingInWaitMultiple = ThreadsMatching(DotNetStacks, "WaitMultiple");
            if (waitingInWaitMultiple != "")
                Console.WriteLine($"The following threads are waiting in a WaitMultiple: \n\t{waitingInWaitMultiple}\n");
        }

        private void AnalyzeThreadsWaitingInWaitOne()
        {
            string waitingInWaitOne = ThreadsMatching(DotNetStacks, "WaitOne");
            if (waitingInWaitOne != "")
                Console.WriteLine($"The following threads are waiting in a WaitOne: \n\t{waitingInWaitOne}\n");
        }

        private void AnalyzeThreadsWaitingForCriticalSection()
        {
            string waitingOnCritSec = ThreadsMatching(NativeStacks, "EnterCriticalSection");
            if (waitingOnCritSec != "")
            {
                Console.WriteLine($"The following threads are waiting for a critical section: \n\t{waitingOnCritSec}\n");
                Console.WriteLine("TIP: Run !sieextPub.critlist to find out who the owner is");
            }
        }

        private void AnalyzeThreadsWaitingToEnterManagedLock()
        {
            string waitingToEnterLock = ThreadsMatching(NativeStacks, "JITutil_MonContention");
            if (waitingToEnterLock != "")
            {
                Console.WriteLine($"The following threads are spinning waiting to enter a .NET Lock: \n\t{waitingToEnterLock}\n");
                Console.WriteLine("TIP: Run !sieextPub.syncblk to find out who the owner is");
            }
        }

        private void AnalyzeThreadsWaitingToEnterLock()
        {
            string waitingToEnterLock = ThreadsMatching(NativeStacks, "JIT_MonTryEnter");
            if(waitingToEnterLock != "")
                Console.WriteLine($"The following threads are spinning waiting to enter a .NET Lock: \n\t{waitingToEnterLock}\n");
        }

        private void PrintLockPosts()
        {
            Console.WriteLine("\nBLOG POSTS ABOUT LOCKS AND CRITICAL SECTIONS");
            Console.WriteLine("==============================================");
            Console.WriteLine("WaitOne and WebService calls \t https://tessferrandez.github.io/debugging/aspnet/hang/2006/02/23/aspnet-performance-case-study-web-service-calls-taking-forever.html");
            Console.WriteLine("Locks and Critical sections \t https://tessferrandez.github.io/debugging/aspnet/hang/2006/01/09/a-hang-scenario-locks-and-critical-sections.html");
        }

        private void AnalyzeGC()
        {
            PrintH2("GC Related information");
            PrintGCThreads();
            AnalyzeThreadsWaitingForGCToComplete();
            AnalyzeGCTryingToSuspendThreads();
            AnalyzeFinalizerBlocked();
            PrintGCPosts();
        }

        private void AnalyzeFinalizerBlocked()
        {
            var cleanFinalizers = ThreadsMatching(NativeStacks, "WaitForFinalizerEvent");
            if (cleanFinalizers == "")
                Console.WriteLine($"The Finalizer (Thread {GetFinalizerThread()}) is blocked\n");
            else
                Console.WriteLine($"The Finalizer (Thread {GetFinalizerThread()}) is NOT blocked\n");
        }

        private int GetFinalizerThread()
        {
            foreach (var thread in DotNetThreads)
                if (thread.Finalizer)
                    return thread.ID;
            return -1;
        }

        private void AnalyzeGCTryingToSuspendThreads()
        {
            var suspendingThreads = ThreadsMatching(NativeStacks, "SysSuspendForGC");
            if (suspendingThreads != "")
            {
                Console.WriteLine("The GC is working on suspending threads to continue with garbage collection");
                var disabledThreads = AnalyzePreemptiveGCDisabled();
                if (disabledThreads != "")
                    Console.WriteLine($"The following threads can't be suspended because preemptive GC is disabled:\n\t{disabledThreads}\n");
            }
        }

        private string AnalyzePreemptiveGCDisabled()
        {
            string threadIDs = "";
            foreach (var thread in DotNetThreads)
                if (!thread.GCEnabled)
                    threadIDs += thread.ID.ToString() + " ";
            return threadIDs;
        }

        private void AnalyzeThreadsWaitingForGCToComplete()
        {
            var threadsWaitingForGC = ThreadsMatching(NativeStacks, "WaitUntilGCComplete");
            if (threadsWaitingForGC != "")
            {
                Console.WriteLine($"The following threads are waiting for the GC to finish:\n\t{threadsWaitingForGC}\n");
                var garbageCollectingThread = ThreadsMatching(NativeStacks, "GarbageCollectGeneration");
                if (garbageCollectingThread != "")
                    Console.WriteLine($"The GC was triggered by threaD: {garbageCollectingThread}\n");
            }
        }

        private void PrintGCThreads()
        {
            var gcThreads = ThreadsMatching(NativeStacks, "gc_thread_stub");
            if (gcThreads != "")
                Console.WriteLine($"The following threads are GC threads:\n\t{gcThreads}\n");
        }

        private string ThreadsMatching(List<CallStack> callStacks, string criteria)
        {
            string threadIDs = "";
            foreach(var stack in callStacks)
                if (stack.Matches(criteria))
                    threadIDs += stack.ID.ToString() + " ";
            return threadIDs;
        }

        private void PrintGCPosts()
        {
            Console.WriteLine("\nBLOG POSTS ABOUT GC RELATED ISSUES");
            Console.WriteLine("===================================");
            Console.WriteLine("GC-LoaderLock Deadlock \t\t https://tessferrandez.github.io/aspnet/debugging/hang/2007/03/12/net-hang-case-study-gc-loader-lock-deadlock.html");
            Console.WriteLine("High CPU in GC \t\t\t https://tessferrandez.github.io/debugging/aspnet/hang/2006/06/22/aspnet-case-study-high-cpu-in-gc.html");
            Console.WriteLine("High CPU in GC (Viewstate) \t https://tessferrandez.github.io/debugging/aspnet/hang/memory/2006/11/24/aspnet-case-study-death-by-viewstate.html");
            Console.WriteLine("Blocked finalizer \t\t https://tessferrandez.github.io/debugging/dotnet/memory/hang/crash/2006/03/26/net-memory-leak-unblock-my-finalizer.html");
        }

        private void ParseInput(string[] lines)
        {
            int numLines = lines.Length;
            int i = 0;

            // Read Native call stacks
            while(i < numLines && !lines[i].Contains("MANAGED THREADS"))
            {
                // RetAddr indicates new thread
                if (lines[i].Contains("RetAddr"))
                {
                    // get the id from the previous line
                    int id = GetNativeThreadId(lines[i - 1]);

                    // read the call stack
                    CallStack cs = new CallStack(id);
                    while (i < numLines && !lines[i].Contains("MANAGED THREADS") && lines[i] != "")
                    {
                        cs.AddFrame(lines[i]);
                        i++;
                    }
                    NativeStacks.Add(cs);
                }
                else
                {
                    i++;
                }
            }

            // Read .net threads
            if (lines[i].Contains("MANAGED THREADS"))
            {
                while (i < numLines && !lines[i].Contains("MANAGED CALLSTACKS"))
                {
                    if (lines[i].Contains("ThreadOBJ"))
                    {
                        i++;
                        while(i < numLines && !lines[i].Contains("MANAGED CALLSTACKS"))
                        {
                            DotNetThreads.Add(new DotNetThread(lines[i]));
                            i++;
                        }
                    }
                    else
                    {
                        i++;
                    }
                }
            }

            // Read .net call stacks
            while (i < numLines)
            {
                if(lines[i].Contains("OS Thread Id"))
                {
                    CallStack cs = new CallStack(GetDotNetThreadId(lines[i]));
                    i++;

                    // add frames
                    while (i < numLines && !lines[i].Contains("OS Thread Id"))
                    {
                        // ignore frames with no managed code
                        if (lines[i].Contains("Failed to start stack walk"))
                        {
                            cs.AddFrame("No managed stack");
                            i++;
                        }
                        else if (lines[i].Contains("Unable to walk"))
                        {
                            cs.AddFrame("No managed stack");
                            i += 3;
                        }
                        else
                        {
                            cs.AddFrame(lines[i]);
                            i++;
                        }
                    }
                    DotNetStacks.Add(cs);
                }
                else
                {
                    i++;
                }
            }
        }

        private int GetDotNetThreadId(string line)
        {
            int start = line.IndexOf('(');
            int end = line.IndexOf(')');
            return int.Parse(line.Substring(start + 1, end - start - 1));
        }

        private int GetNativeThreadId(string line)
        {
            var tokens = line.Split(' ');
            if (tokens[1] != "") return int.Parse(tokens[1]);
            if (tokens[2] != "") return int.Parse(tokens[2]);
            return int.Parse(tokens[3]);
        }

        private void PrintHeader(string header)
        {
            Console.WriteLine("=====================================================");
            Console.WriteLine(header.ToUpper());
            Console.WriteLine("=====================================================");
        }

        private void PrintH2(string header)
        {
            Console.WriteLine("-----------------------------------------------------");
            Console.WriteLine(header.ToUpper());
            Console.WriteLine("-----------------------------------------------------");
        }
    }
}
