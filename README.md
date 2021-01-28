# Hang Analyzer 

by: Tess Ferrandez - https://tessferrandez.github.io/

## Overview

Tool for automatic hang analysis of .net memory dumps - This was orignally written 2007 to speed up hang debugging while I was working in developer support.
Since then, most of these concepts were incorporated in the tool [Debug Diag](https://www.microsoft.com/en-us/download/confirmation.aspx?id=58210).

I am currently in the process of restoring my old blog, and therefore I am also adding these small tools and utilities to github, but for all your analysis needs, use debug diag instead.

## Usage

This application expects the input 
```cmd
~* kb 2000;.echo MANAGED THREADS;!threads;.echo MANAGED CALLSTACKS;~* e !clrstack
```

To run from windbg or cdb use  

```cmd
.shell -ci "~* kb 2000;.echo MANAGED THREADS;!threads;.echo MANAGED CALLSTACKS;~* e !clrstack" HangAnalyzer
```

## Analysis details 

Hang Analyzer will look for the following hangs

### GC Related

- gc_thread_stub - GC Threads
- SysSuspendForGC - Trying to suspend threads for GC
- WaitForFinalizerEvent - If found, the finalizer is not blocked
- WaitUntilGCComplete - Waiting for GC to finish
- GarbageCollectGeneration - Thread triggering the GC

### Locks

- JIT_MonTryEnter - In spinn loop waiting for a managed lock 
- EnterCriticalSection - Waiting for critical section - Run !SiextPub.CritList to find owner
- JITutil_MonContention - Entering a managed lock - Run !sos.syncblk to find the owner
- WaitOne - synchronization mechanism for async calls (used by webservices)
- WaitMultiple - synchronization mechanism for async calls
 
### External Calls

- Socket.Receive - Waiting on results returned on a socket
- SendReceive2 - Waiting on com call - Run !SiextPub.ComCalls for more info
- GetToSTA - Waiting for STA thread - Run !SiextPub.ComCalls for more info
 
Add your own checks in Analyzer.Analyze
 