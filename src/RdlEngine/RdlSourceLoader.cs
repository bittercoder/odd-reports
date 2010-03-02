using System.IO;

namespace RdlEngine
{
  public interface IRdlSourceLoader
  {
    string GetRdlSource(string path);
  }

  public class RdlSourceLoader : IRdlSourceLoader
  {
    #region IRdlSourceLoader Members

    public string GetRdlSource(string path)
    {
      StreamReader fs = null;
      string prog = null;
      try
      {
        fs = new StreamReader(path);
        prog = fs.ReadToEnd();
      }
      finally
      {
        if (fs != null)
          fs.Close();
      }

      return prog;
    }

    #endregion
  }
}