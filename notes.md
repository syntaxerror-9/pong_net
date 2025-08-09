---
MacOS system headers are in /Library/Developer/CommandLineTools/SDKs/MacOSX.sdk/usr/include/sys

(Since im using posix sockets, i need to find the enum values)
---

Since i'll be using UDP, some packets will have to be acknowledged by the receiver. (i could use TCP for these packets
but i dont want to overcomplicate it)

Communication protocol Client:

| Opcode | Message         | Sender        | Payload                                                                             | Ack |
|--------|-----------------|---------------|-------------------------------------------------------------------------------------|-----|
| 0x00   | Joining         | Client        | 0                                                                                   | yes |
| 0x01   | Exit            | Client        | 0                                                                                   | yes |
| 0x02   | Move Paddle     | Client        | PaddleY: int                                                                        | no  |
| 0x03   | Ball state      | Server        | VelocityX: float,VelocityY: float,PositionX: float,PositionY: float,Timestamp: long | no  |  
| 0x04   | Update score    | Server        | P1: int,P2: int                                                                     | yes |
| 0x05   | EnemyMovePaddle | Server        | Enemy paddle position: int                                                          | no  |
| 0x06   | Echo            | Server        | Echos the last opcode that was sent by client: byte                                 | yes |
| 0x07   | PlayerIndex     | Server        | Sends the player Index: byte[0,1]                                                   | yes |
| 0x08   | MatchEnd        | Server        | Sends the player the result of the match: byte[0,1]-0=loose,1=win                   | yes |
| 0xFF   | Ack             | Client/Server | Sends an acknowledgment with the last opcode                                        | no  |

Opcode will be the first byte.
PacketNumber will be the second byte.
Payload will be third byte+;

