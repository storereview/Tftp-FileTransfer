# Tftp-FileTransfer

Tftp 协议只支持简单的文件传输服务，因此大多数远程工具大多使用 ftp 或是 sftp 协议来传输文件。在一类特殊的嵌入式设备中，文件传输协议只有 tftp 协议。

tftp 无法提供目录相关的信息。这里使用 telnet 来提供文件树的功能。

