# SmartFoxServer Example Projects for Godot 4.x
The series of **C# examples** built for the Godot 4 engine have been developed with **Godot Mono 4.0.3**, but although this example uses Godot Mono 4.2.1, the concepts and the code to interact with the SFS2X API are valid for any version of Godot 4.x (unless otherwise noted).

Each of the tutorials in this series examine a single example, describing its objectives, **offering an insight into the SmartFoxServer features** it wants to highlight. This project includes all the assets required to compile and test the example (both client and — if existing — server side). If necessary, code excerpts are provided in the tutorial itself (see online documentation link below), in order to better explain the approach that was followed to implement a specific feature. At the bottom of the tutorial, additional resources are linked if available.

The tutorials have an increasing complexity, from basic server connection to a complete game with authoritative server code.

Specifically, the examples will showcase:

* basic connection with optional protocol encryption
* room management
* buddy list management
* game rooms and match-making
* **authoritative server in a turn-based game**

The Godot examples provided have been tested for exporting as native executables for Windows and macOS. At the time of writing this article (June 2023) Godot Mono does not yet support exporting for mobile platform or the browser.



# SFS_Shooter_GD4
The Shooter example shows how to develop a full multiplayer first/third person combat game with Godot 4.x and SmartFoxServer 2X. The game utilizes SmartFoxServer 2X's ability to mix TCP and UDP based messaging, and makes full use of SmartFoxServer's Lag Monitor.

TCP is the most common protocol deployed on the Internet. Its dominance is explained by the fact that TCP performs error correction. When TCP is used, there is an assurance of delivery. This is accomplished through a number of characteristics including ordered data transfer, retransmission, flow control and congestion control. During the delivery process, packet data may collide and be lost, however TCP ensures that all packet data is received by re-sending requests until the complete package is successfully delivered.

UDP is also commonly found on the Internet. However UDP is not used to deliver critical information, as it forgoes the data checking and flow control found in TCP. For this reason, UDP is significantly faster and more efficient, although, it cannot be relied on to reach its destination.

In this example, all critical game information between the server and client are sent via TCP for reliable delivery. This could be shooting for example, or animation sync, damage messages, etc. On the other hand, sending updates to the players Transform, which is continuously sent by and to all clients, is executed using UDP.

The example scene features a simple lobby and multiple games which can run at the same time as individual arenas. We have built this example with Godot 4.2, but it is also compatible with other 4.x versions. This example was ported from its Unity counterpart. However, the Character Transform is different than the Unity example, and although the server-side code is the same, a Unity built client cannot play the same game as a Godot built client.

» Client code
This example is not meant to be a tutorial on how to write a 3rd person shooter in Godot. It is designed to showcase the features of SmartFoxServer, and how to utilize TCP and UDP in the same project. However, this example is extensive and could be used as the basis of a 1st or 3rd person multiplayer game and as such, we will cover the main components of the Client code, but more specifically, how they relate to the Server Extension.

The main components in the client project are found in the Scripts folder. They are as follows:

The GameManager is the main component that is attached to the Game scene. The main configurable variables are presented in the Inspector, including Player Model selection, Player Model Colors, and UI elements.
The CharacterTransform is used to position the characters in the world simulation, including character rotation, and weapon aiming. The local player sends this transform object to the server script, which in turn it is propagated to other players for synchronization.
The Player script is utilized in each of the player scenes. It controls movement, animations, for the local character, and positioning and animations are synchronized for the remote characters.
» Server code
The Java Extension includes Handlers to communicate with the client, and Simulation code to handle the player transforms and positions within the world simulation. This means that the world simulation runs on the server side, and all clients have a representative simulation running on their system. Clients communicate with the game code on the server side via extension requests and gets data back via extension response. The data that is sent and received using SFSObjects, which are highly optimized dataobjects that can be nested. The main sections in the server code are as follows:

The Shooter Extension main class is used to handle requests from clients and send messages back to them. All communication is done with json objects
The Shooter Utils includes the Room Helper and User Helper, which handle the Room/Zone responses and User response respectively.
The Shooter Handlers send and receive Objects to and from the client. These include the transform itself, as well as the spawning, shooting, and animations.
The World Simulation includes code to control the Player transforms, spawned Items, Weapon shooting, loading, and reloading, and Game Variables.
