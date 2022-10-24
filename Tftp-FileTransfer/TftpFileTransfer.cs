using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tftp_FileTransfer.Protocol_Implementation;


namespace Tftp_FileTransfer
{
    public class TftpFileTransfer
    {
        TelnetConnection tc;
        public void Start()
        {
            tc = TelnetClient.CreateTelnetClient("128.128.12.71", "root", "jvtsmart123");
        }
    }
}
