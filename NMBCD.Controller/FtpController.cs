using System.Net;
using System.Net.FtpClient;
using System.Threading;

namespace NMBCD.Controller
{
    public class FtpController
    {
        private ManualResetEvent m_reset = new ManualResetEvent(false);
        private readonly string _host;
        private readonly string _user;
        private readonly string _pass;

        public FtpController(string host, string user, string password)
        {
            _host = host;
            _user = user;
            _pass = password;
        }

        public bool FileExist(string location)
        {
            bool result = false;

            try
            {
                using (FtpClient conn = new FtpClient())
                {
                    conn.Host = _host;
                    conn.Credentials = new NetworkCredential(_user, _pass);

                    if (conn.FileExists(location,
                        FtpListOption.ForceList | FtpListOption.AllFiles))
                    {
                        result = true;
                    }
                }
            }
            catch { }

            return result;
        }
    }
}