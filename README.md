A simple multi-client echo server built using TcpClient and TcpListener from the .NET framework.
Message framing is handled via a length prefix, the first four bytes after the first byte indicate the length of the payload. Application-level keep-alives (heartbeats) are also implemented to detect half-open connections.
The type of each message (normal or heartbeat) is indicated by the first byte of the stream (yes it's a bit redundant).
