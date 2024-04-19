using Microsoft.AspNetCore.Mvc;

namespace UpdateServer.Model
{
    public record class NewVersionData(string Program, string Version,
         IFormFile SourceFile, IFormFile InstallFile, IFormFile Changelog);
}
