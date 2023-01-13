using CommandLine;

namespace Tracert
{
    public class Options
    {
        [Value(index: 0, Required = true, MetaName = "target_name",
            HelpText = "Specifies the destination, identified either by IP address or host name.")]
        public string TargetName { get; set; } = null!;
    }
}
