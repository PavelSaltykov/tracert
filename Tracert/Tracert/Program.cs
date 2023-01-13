using CommandLine;

namespace Tracert;

class Program
{
    static void Main(string[] args)
    {
        Parser.Default.ParseArguments<Options>(args).WithParsed(Run);
    }

    static void Run(Options opts) => 
        TracertUtility.PrintTraceRoute(opts.TargetName);
}