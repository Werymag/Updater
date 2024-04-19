namespace UpdateServer.ViewModel
{
    public class VersionViewModel
    {
        public VersionViewModel(string program)
        {
            this.Program = program;
        }

        public string Program { get; set; }
        public List<ProgramVersionInfo> Versions { get; set; } = new List<ProgramVersionInfo>();
    }

    public record class ProgramVersionInfo(string Version, string Changelog);

}
