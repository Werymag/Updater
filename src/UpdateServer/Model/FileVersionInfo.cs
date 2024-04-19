namespace UpdateServer.Model;

public class AllFilesVersionInfo : List<FileVersionInfo>
{ }

public record class FileVersionInfo(string FileName, string Md5Hash);
